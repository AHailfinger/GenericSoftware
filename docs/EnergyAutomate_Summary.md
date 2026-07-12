# EnergyAutomate Project Summary

## Overview
`EnergyAutomate` is the core application of the EnergyAutomate ecosystem. It serves as the central hub for managing energy data, calculating energy adjustments, and integrating with various energy providers and hardware devices.

## Key Components
- **Data Layer**: Manages persistence for application settings, user data, and energy measurements using `ApplicationDbContext`. Includes entities like `CodeTemplate` and `CodeTemplateHistory` for versioned script management.
- **Services**: 
    - `EnergyAutomateService`: Core service for energy management and RTM (Real-Time Measurement) adjustments.
    - `TIbberApiService`: Handles integration with the Tibber energy provider, including real-time measurement processing and lifecycle management.
    - `GrowattApiService`: Provides interaction with Growatt inverters.
- **Code Factory**: A specialized subsystem for defining, storing, and executing energy calculation and adjustment scripts as versioned code templates.
- **API & Controllers**: Exposes endpoints for managing templates and retrieving energy data.
- **Watchdogs**: Monitoring services for ensuring system reliability and handling reconnects.

## Key Features & Implementation
### Code Template Versioning
- **Persistence**: `CodeTemplate` and `CodeTemplateHistory` tables store versioned scripts with SHA256 hashing to ensure only actual changes are versioned.
- **Workflow**: 
    - **Startup**: `RuntimeCodeTemplateStore` initializes templates from embedded defaults and loads them into an in-memory cache.
    - **Saving**: `DatabaseCodeTemplateStore` handles asynchronous background saving with `ChangeNotes` and user tracking.
    - **Rollback**: Supports jumping to any previous version, creating a new version entry to maintain a full audit trail.
- **Execution**: `RuntimeCodeTemplateExecutor` and `RoslynCodeFactory` allow for dynamic execution of C# scripts.

### Energy Adjustment Logic
- **Adaptive Hysteresis**: The `AdjustPower` method uses a dynamic hysteresis based on the difference between current and target consumption to prevent excessive frequent adjustments.
- **Smooth Adjustment**: Employs a logarithmic adjustment factor to ensure smooth transitions in power levels.
- **Hardware Awareness**: Ensures power values are divisible by the number of connected devices to guarantee equal distribution.
- **Decision Hierarchy**:
    - **Auto Mode**: Highest priority; automatically manages power distribution.
    - **Extension Mode**: Active during specific times/conditions, prioritizing cheap price modes and battery preservation.
    - **Normal Mode**: Default state; optimizes for direct solar usage and battery charging/discharging based on price and SoC.

## Core Energy Management Logic & Requirements

This section consolidates detailed functional requirements and core adjustment methods for the energy management system.

### ⚙️ Power Adjustment Method (From AdjustPower_Method_Documentation)
The method `AdjustPower` passes the current power value by adjusting it to optimize energy consumption based on a target setting, using an adaptive hysteresis mechanism. The process involves:
1. Calculating the difference between current and target consumption.
2. Determining the last committed power value.
3. Applying dynamic hysteresis based on the absolute difference.
4. Calculating an adjustment factor logarithmically to ensure smooth adjustments.
5. Computing a new Power Value, ensuring it respects the maximum allowed power limit (`SettingMaxPower`).
6. Crucially, adjusting the final Power Value so that it is perfectly divisible by the total number of connected devices, ensuring fair load distribution.

### ⚡ Decision Logic (From ApiServiceRTMAdjustment3)
The system uses a strict decision hierarchy to determine energy management action:
#### Auto Mode Check (Highest Priority)
- If `ApiSettingAutoMode = true`, the process executes `TibberRTMAdjustment3AutoMode(value)` and stops.

#### Normal Operation Flow
1. **Price Price Check**: Prioritizes charging if cheap prices are detected or activating energy saving mode if expensive prices are detected.
2. **Solar Usage Optimization**: Prioritizes direct solar usage unless explicitly restricted by pricing.
3. **Battery Charging**: Prioritizes battery charging if conditions (low price, low SoC) are met.
4. **Default Fallback**: Otherwise, runs an optimal distribution based on current measurements.

### Power Adjustment Detail (`TibberRTMAdjustment3SetPower`)
This function manages power limits and load balancing:
- If total power exceeds `ApiSettingMaxPower`, the excess is capped or distributed evenly (if negative).
- Load balancing occurs within hysteresis ranges to maintain stability without changing the total required power.

## Technologies
- .NET 8+
- Entity Framework Core
- Blazor
- SQL Server/SQLite (via DbContext)
- MQTT / Modbus (via Emulators)

## Functional Requirements Checklist (From Requirements)
1. **Language**: All documentation, logs, and code comments MUST be in English.
2. **Device Power Limits**: Each device is limited between $0\text{ W}$ and $\text{ApiSettingMaxPower} / \text{device count}$. The sum of all must respect the total limit ($\text{ApiSettingMaxPower}$).
3. **Grid Feed-in Handling**: Special rules apply if `TotalPower` (grid draw) is negative or positive, forcing device power to $0\text{ W}$ or $\text{ApiSettingMaxPower} / \text{device count}$, respectively, when exceeding limits.
4. **Hysteresis**: Changes are avoided within a configurable hysteresis range unless necessary for load balancing.
5. **Grid Draw Regulation**: The system calculates `delta = TotalPower - ApiSettingAvgPowerOffset` and distributes the resulting correction power (`adjustedDelta`) to devices proportional to their SoC priority (high for positive delta, low for negative delta).

**Actionable Takeaway:** All adjustments are constrained by physical limits, load balancing rules based on SoC, and clear operational modes defined by price signals.
