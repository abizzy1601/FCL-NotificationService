using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Config;
using NotificationService.Application.UseCases.Command.CreateNotification;
using NotificationService.Core.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationService.Infrastructure.Messaging.RabbitMq
{
    /// <summary>
    /// Envelope shape published by RabbitMqPublisher.PublishAsync (both here and in
    /// the Worker): { "data": { ...NotificationEvent... } }.
    /// </summary>
    internal class NotificationMessageEnvelope
    {
        [JsonPropertyName("data")]
        public NotificationEvent? Data { get; set; }
    }

    /// <summary>
    /// Listens on a single named queue (RabbitMqSettings.NotificationQueueName) —
    /// deliberately no topic exchange, no routing-key patterns, no dead-letter
    /// topology, mirroring how minimal the Worker's own RabbitMQ usage is. If you
    /// need multiple event categories routed to different places later, the
    /// NotificationEvent.Category field is already there to branch on inside
    /// OnMessageReceivedAsync rather than reaching for exchange complexity again.
    /// </summary>
    public class NotificationConsumer : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly RabbitMqSettings _settings;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationConsumer> _logger;

        public NotificationConsumer(
            IConnection connection,
            IOptions<RabbitMqSettings> settings,
            IServiceScopeFactory scopeFactory,
            ILogger<NotificationConsumer> logger)
        {
            _connection = connection;
            _settings = settings.Value;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var channel = _connection.CreateModel();

            channel.QueueDeclare(
                queue: _settings.NotificationQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            channel.BasicQos(prefetchSize: 0, prefetchCount: 20, global: false);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += async (_, ea) => await OnMessageReceivedAsync(channel, ea, stoppingToken);

            channel.BasicConsume(queue: _settings.NotificationQueueName, autoAck: false, consumer: consumer);

            _logger.LogInformation("NotificationConsumer listening on queue {Queue}", _settings.NotificationQueueName);

            return Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
        }

        private async Task OnMessageReceivedAsync(IModel channel, BasicDeliverEventArgs ea, CancellationToken ct)
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var envelope = JsonSerializer.Deserialize<NotificationMessageEnvelope>(json);
                var evt = envelope?.Data;

                if (evt is null || string.IsNullOrWhiteSpace(evt.EventId))
                {
                    _logger.LogWarning("Malformed NotificationEvent — dropping and acking (nothing to retry)");
                    channel.BasicAck(ea.DeliveryTag, multiple: false);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var command = new CreateNotificationCommand(
                    evt.EventId, evt.UserId, evt.Category, evt.Channel, evt.Priority,
                    evt.Title, evt.Body, evt.Metadata, evt.ActionUrl);

                var result = await mediator.Send(command, ct);

                // Conflict = already-processed EventId (idempotent no-op), not a failure.
                if (result.IsFailure && result.Error.Code != "Conflict")
                {
                    _logger.LogError(
                        "CreateNotificationCommand failed for EventId={EventId}: {Error}",
                        evt.EventId, result.Error.Message);
                    // No DLX configured — nack without requeue so a permanently-bad
                    // message doesn't loop forever; check logs to diagnose/replay manually.
                    channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    return;
                }

                channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize message — dropping (not requeued)");
                channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error processing message — requeueing once");
                channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: !ea.Redelivered);
            }
        }
    }
}
