# EnergyAutomate.Emulator Project Summary

## Overview
The `EnergyAutomate.Emulator` project provides a simulation environment for testing energy management logic without requiring physical hardware. It simulates various devices and communication protocols.

## Key Components
- **Python Simulation**: Contains Python scripts for device simulation:
    - `Python/PythonGrowattClient.py`: Simulates Growatt inverter behavior.
    - `Python/PythonWrapper.cs`: A .NET wrapper for executing Python scripts and handling communication.
- **Device Support**:
    - **Growatt**: Simulated via Python scripts.
    - **Shelly**: Supported via the `Shelly/` directory for simulation or interaction.
- **Communication Protocols**:
    - **Modbus/MQTT**: Generates Modbus payloads forwarded via MQTT to simulate real-world telemetry.

## Technologies
- .NET 8+
- Python
- MQTT
- Modbus RTU
