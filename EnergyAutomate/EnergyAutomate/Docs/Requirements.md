# Requirements

1. **Language**
   - All code comments, log messages, summaries, and documentation must be in **English**.

2. **Device Power Limits**
   - Each battery device (`DeviceList`) can be set between **0 W and ApiSettingMaxPower / device count**.
   - No device may ever be set to a negative power value.
   - The sum of all device power values is limited to **ApiSettingMaxPower** (e.g., 840 W for 2 batteries).

3. **Grid Feed-in Handling**
   - If `TotalPower` (grid draw) is **negative** and Abs(TotalPower) exceeds ApiSettingMaxPower, all device power values must be set to **0 W**.
   - If `TotalPower` (grid draw) is **positive** and Abs(TotalPower) exceeds ApiSettingMaxPower, all device power values must be set to **ApiSettingMaxPower / device count**.
   - In all other cases, power regulation should take place to keep the grid draw close to target values.

4. **Hysteresis**
   - If the deviation from the offset is within a configurable **hysteresis range** (e.g., ±10 W),
   all device power values must remain unchanged to avoid unnecessary switching. However, load balancing should still take place.

5. **Grid Draw Regulation**
   - The algorithm aims to keep `TotalPower` as close as possible to the configured offset (`ApiSettingAvgPowerOffset`).
   - The correction power is calculated as: `int delta = value.TotalPower - ApiSettingAvgPowerOffset;`
   - If `delta` is positive, power is distributed to eligible devices to increase grid draw.
   - If `delta` is negative, power is distributed to eligible devices to decrease grid draw.
   - Power allocation per device is limited between **0 W and ApiSettingMaxPower / device count**.
   - Power is distributed according to device priority (SoC-based):
     - For positive delta: devices with higher SoC are prioritized
     - For negative delta: devices with lower SoC are prioritized
     - When SoC values are equal, power is distributed equally.
   - Only non-empty devices are eligible for power allocation. Devices that are empty (i.e., `IsBatteryEmpty == true`) are excluded and set to **0 W**.
   - **When calculating required power changes, the algorithm must always consider the current committed power values per device (PowerValueCommited).**

6. **Adjustment Factor and Power Calculation**
   - The setting **ApiSettingPowerAdjustmentFactor** must be used to scale the delta for power regulation.
   - A value of 100 represents 100% of delta, and must be divided by 100 in the formula: `adjustedDelta = delta * (ApiSettingPowerAdjustmentFactor / 100.0)`
   - `CurrentState.PowerValueTotalCommited` contains the current total power value assigned to devices.
   This is the basis for calculating power distribution using the adjustment factor.
   - If `CurrentState.PowerValueTotalCommited` is negative or NULL, it should be set to 0.
   - For positive deltas: `int desiredTotalPower = Math.Min(powerValueTotalCommited + adjustedDelta, ApiSettingMaxPower);`
   - For negative deltas: `int desiredTotalPower = Math.Max(0, powerValueTotalCommited + adjustedDelta);`
   - **Power Distribution Strategy:**
     - If the delta can be handled by adjusting a single device without exceeding its limits (0 to ApiSettingMaxPower/device count), prioritize that approach.
     - When selecting a single device for adjustment, respect the SoC prioritization rules in point 5.
     - A device's adjustment capacity is defined by:
       - For increases: Maximum is ApiSettingMaxPower/device count - PowerValueCommited
       - For decreases: Maximum is PowerValueCommited (cannot go below 0)
     - Only when the delta cannot be handled by a single device should power be rebalanced across multiple devices.
     - When rebalancing across multiple devices, the new distribution should attempt to minimize changes from current PowerValueCommited values where possible.

7. **SoC-based Load Balancing**
   - Power is distributed among eligible devices **proportional to their SoC priority**.
   - The allocation per device is limited between **0 W and ApiSettingMaxPower / device count**.
   - When within the hysteresis range, load balancing should still occur to equalize battery levels.

8. **Comprehensive Logging**
   - All decisions, calculations, allocations, and actions must be logged in English.
   - Logs should include device serial, SoC, allocation, changes, and reasons for setting power or skipping devices.
   - Each significant step in the process should be logged with appropriate context information.

9. **General**
   - Ensure robustness against all edge cases (no eligible devices, all empty, negative power, etc.).
   - Under no circumstances should negative device power values be set.
   - All code comments must be in English, in addition to log messages.
   - PowerValueRequested is not allowed to set it will be set after EnqueueAsync running successfuly

