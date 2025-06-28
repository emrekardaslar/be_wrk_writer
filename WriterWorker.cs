// be_wrk_writer/WriterWorker.cs
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

// --- USING DIRECTIVES FOR core_lib_messaging ---
using core_lib_messaging.Models;
using core_lib_messaging.RabbitMq;
using core_lib_messaging.Serialization;
// --- END USING DIRECTIVES ---

namespace be_wrk_writer
{
    public class WriterWorker : BackgroundService
    {
        private readonly ILogger<WriterWorker> _logger;
        private readonly IRabbitMqService _rabbitMqService;

        public WriterWorker(ILogger<WriterWorker> logger, IRabbitMqService rabbitMqService)
        {
            _logger = logger;
            _rabbitMqService = rabbitMqService;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("be_wrk_writer: Writer Worker starting...");

            try
            {
                // --- CHANGED: Declare the new request queue for the writer ---
                _rabbitMqService.DeclareQueueWithDeadLetter(RabbitMqConfig.ReqWriterQueue);
                // --- NEW: Declare the new response queue for the writer ---
                _rabbitMqService.DeclareQueueWithDeadLetter(RabbitMqConfig.ResWriterQueue);

                // --- CHANGED: Set up Consumer for Writer Commands (not Feeder Responses) ---
                _rabbitMqService.Consume<WriterCommand>(RabbitMqConfig.ReqWriterQueue, OnWriterCommandReceived, autoAck: false);

                _logger.LogInformation($"be_wrk_writer: Listening for commands on '{RabbitMqConfig.ReqWriterQueue}'");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "be_wrk_writer: Error starting Writer Worker. Shutting down.");
                throw;
            }

            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("be_wrk_writer: Writer Worker running. Press Ctrl+C to stop.");
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            _logger.LogInformation("be_wrk_writer: Writer Worker detected cancellation request.");
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("be_wrk_writer: Writer Worker stopping...");
            _logger.LogInformation("be_wrk_writer: Writer Worker stopped gracefully.");
            return base.StopAsync(cancellationToken);
        }

        // --- CHANGED: Now consumes WriterCommand, renamed method ---
        private async Task OnWriterCommandReceived(WriterCommand? command, MessageDeliveryContext context)
        {
            WriterResponse response = new WriterResponse { CorrelationId = command.CorrelationId };

            if (command == null || string.IsNullOrEmpty(command.DataToPersist))
            {
                _logger.LogWarning("be_wrk_writer: [!] Error: Received null or empty WriterCommand. Nacking.");
                response.IsSuccess = false;
                response.ErrorMessage = "Received null or empty data to persist.";
                _rabbitMqService.Nack(context.DeliveryTag, requeue: false);
                await _rabbitMqService.PublishAsync(RabbitMqConfig.ResWriterQueue, response); // Publish error response
                return;
            }

            _logger.LogInformation($"be_wrk_writer: [x] Processing WriterCommand (CorrelationId: {command.CorrelationId})");

            try
            {
                // Simulate writing data to a database/storage
                await Task.Delay(1000); // Simulate I/O operation

                // Simulate a random failure (e.g., 10% chance)
                if (new Random().Next(1, 101) <= 10)
                {
                    throw new InvalidOperationException("Simulated database write error!");
                }

                string newId = Guid.NewGuid().ToString(); // Simulate getting an ID after write
                _logger.LogInformation($"be_wrk_writer: [V] Data '{command.DataToPersist}' simulated written with ID: {newId}");

                response.IsSuccess = true;
                response.PersistedItemId = newId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"be_wrk_writer: [!] Error writing data for CorrelationId: {command.CorrelationId}.");
                response.IsSuccess = false;
                response.ErrorMessage = $"Failed to persist data: {ex.Message}";
            }
            finally
            {
                // Acknowledge the received command message
                _rabbitMqService.Ack(context.DeliveryTag);

                // Publish the response to the Orchestrator (or next service)
                await _rabbitMqService.PublishAsync(RabbitMqConfig.ResWriterQueue, response);
            }

            _logger.LogInformation($"be_wrk_writer: [x] Completed processing for CorrelationId: {response.CorrelationId}. IsSuccess: {response.IsSuccess}");
        }
    }
}