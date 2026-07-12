# Tibber Service Integration (From TIBBER_SERVICE & TibberApiService_Details)

## Overview
The `EnergyAutomate.Tibber` project handles the integration with the Tibber energy provider. It is responsible for fetching real-time energy prices, measurements, and managing the lifecycle of the Tibber listener.

## Main Responsibilities

- validate the Tibber API token
- load Tibber home data when needed
- start the Tibber real-time measurement listener
- stop the Tibber real-time measurement listener
- process incoming real-time measurements
- execute calculation and adjustment templates
- store and update Tibber measurements in the application state
- trigger UI refreshes after new measurement data arrives

## Key Components
The system relies on several key classes: `TIbberApiService` (the central manager), `TibberBackgroundService` (`IHostedService`), and `RealTimeMeasurementObserver`. This architecture separates concerns, keeping the core service logic clean.

- **TIbberApiService**: The central service that owns the Tibber lifecycle and measurement processing. It manages:
    - Service lifecycle state
    - Listener startup and shutdown
    - Restart handling
    - Processing of real-time measurements
    - Execution of calculation and adjustment templates
- **TibberBackgroundService**: A thin wrapper implementing `IHostedService` that delegates all lifecycle calls to `TIbberApiService`.
- **RealTimeMeasurementObserver**: Receives measurements from the listener and forwards them to `TIbberApiService`.
- **TibberApiClient**: Communicates with the Tibber REST API for home data and real-time measurement streams.
- **Models**: Includes `TibberPrice`, `TibberRealTimeMeasurement`, and `RealTimeMeasurement` for internal data representation.

## Technologies
- .NET 8+
- HttpClient
- MQTT / WebSockets (for real-time feeds)
