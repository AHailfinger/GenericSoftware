# EnergyAutomate.Growatt Project Summary

## Overview
The `EnergyAutomate.Growatt` project is a dedicated library for interacting with Growatt inverters. It contains the data models, API clients, and query logic required to fetch device information and telemetry.

## Key Components
- **Device Models**: Extensive set of classes for various Growatt device types (e.g., `DeviceMinInfoData`, `DeviceNoahInfoData`, `DeviceNoahSetPowerQuery`).
- **API Clients**: `GrowattApiClient.cs` handles the raw HTTP communication with Growatt's cloud APIs.
- **Query Builders**: Provides structured ways to build queries for device lists, historical data, and configuration settings.
- **Data Responses**: Specific models for various data types including `DeviceMinLastData`, `DeviceNoahHistoricalData`, and `DeviceNoahSetLowLimitSocQuery`.

## Technologies
- .NET 8+
- HttpClient
- JSON.NET
