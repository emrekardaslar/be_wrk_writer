# be_wrk_writer

This project is the Writer service for the OrchestrationDemo solution. It receives commands from the orchestrator, simulates persisting data, and responds with the results via RabbitMQ.

## Features

- Listens for `WriterCommand` messages from the orchestrator
- Simulates writing data to a database or storage system
- Publishes `WriterResponse` messages with the result of the write operation
- Simulates random write failures for testing error handling
- Handles malformed or null commands robustly
- Logs all major events and errors

## Project Structure

- `WriterWorker.cs` - Main background service implementing the writer logic
- `Program.cs` - Application entry point and host configuration
- `appsettings.json` / `appsettings.Development.json` - Configuration files for RabbitMQ and other settings

## Running the Service

1. Ensure RabbitMQ is running and accessible as configured in `appsettings.json`.
2. Build and run the project:
   ```sh
   dotnet run
   ```
3. The writer will listen for commands and respond with simulated persistence results.

## Configuration

Edit `appsettings.json` or `appsettings.Development.json` to set RabbitMQ connection details and other settings.

## Development

- .NET 8.0
- Uses dependency injection and background services
- Requires the `core_lib_messaging` library for RabbitMQ integration

## Related Projects

- `be_wrk_orchestrator` - Orchestrator service
- `be_wrk_feeder` - Feeder service
- `core_lib_messaging` - Shared messaging library

---

**Note:**  
This writer is designed for demo and development purposes. In production, replace the simulated persistence logic with real database/storage operations and add more robust error handling as needed.