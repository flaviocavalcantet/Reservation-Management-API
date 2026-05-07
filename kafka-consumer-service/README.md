# Kafka Consumer Service

This project implements a Kafka consumer as a background service in an ASP.NET Core application. It subscribes to reservation events, processes messages safely, and ensures idempotency to prevent duplicate processing. The service also handles retries and failures while logging the processing lifecycle.

## Project Structure

- **src/**: Contains the main application code.
  - **Program.cs**: Entry point of the application, configures the host and registers the Kafka consumer hosted service.
  - **Services/**: Contains services related to Kafka message processing.
    - **KafkaConsumerHostedService.cs**: Implements `IHostedService` to manage Kafka consumer lifecycle.
    - **IReservationEventProcessor.cs**: Interface for processing reservation events.
    - **ReservationEventProcessor.cs**: Implements `IReservationEventProcessor` to handle reservation events.
  - **Models/**: Contains data models used in the application.
    - **ReservationEvent.cs**: Represents the structure of a reservation event message.
    - **ProcessingState.cs**: Tracks the state of message processing.
  - **Infrastructure/**: Contains infrastructure components for idempotency and retry logic.
    - **IIdempotencyStore.cs**: Interface for managing idempotency keys.
    - **IdempotencyStore.cs**: Implements `IIdempotencyStore` to prevent duplicate processing.
    - **IRetryPolicy.cs**: Interface for implementing retry logic.
    - **RetryPolicy.cs**: Implements `IRetryPolicy` for handling retries and failures.
  - **Logging/**: Contains logging components for the Kafka consumer.
    - **ConsumerLogger.cs**: Handles logging for the Kafka consumer.

- **tests/**: Contains unit tests for the application components.
  - **KafkaConsumerServiceTests.cs**: Tests for `KafkaConsumerHostedService`.
  - **ReservationEventProcessorTests.cs**: Tests for `ReservationEventProcessor`.
  - **IdempotencyStoreTests.cs**: Tests for `IdempotencyStore`.

- **appsettings.json**: Configuration settings for the application, including Kafka connection details and logging settings.

- **kafka-consumer-service.csproj**: Project file specifying dependencies and build settings.

## Setup Instructions

1. Clone the repository:
   ```
   git clone <repository-url>
   cd kafka-consumer-service
   ```

2. Restore the dependencies:
   ```
   dotnet restore
   ```

3. Update the `appsettings.json` file with your Kafka connection details.

4. Run the application:
   ```
   dotnet run
   ```

## Usage

The Kafka consumer will start automatically and begin processing reservation events from the configured Kafka topic. Ensure that your Kafka broker is running and accessible.

## Logging

The application logs the processing lifecycle of messages, including successes and failures. Check the logs for detailed information about the message processing status.

## Testing

Run the unit tests to validate the functionality of the application:
```
dotnet test
```

## Contributing

Contributions are welcome! Please submit a pull request or open an issue for any enhancements or bug fixes.