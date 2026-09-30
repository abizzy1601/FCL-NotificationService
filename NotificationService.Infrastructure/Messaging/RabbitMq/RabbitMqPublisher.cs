using System.Text;
using System.Text.Json;
using NotificationService.Application.Services;
using RabbitMQ.Client;

namespace NotificationService.Infrastructure.Messaging.RabbitMq
{
    /// <summary>
    /// Line-for-line the same as NotificationService.Worker.Services.RabbitMqPublisher
    /// (default exchange, routingKey == queueName, { data: message } envelope) —
    /// kept identical on purpose so both projects publish the same way and either
    /// one's messages can be read/replayed with the same tooling.
    /// </summary>
    public class RabbitMqPublisher : IRabbitMqPublisher
    {
        private readonly IConnection _connection;

        public RabbitMqPublisher(IConnection connection)
        {
            _connection = connection;
        }

        public Task PublishAsync<T>(string queueName, T message)
        {
            using var channel = _connection.CreateModel();

            var notificationMessage = new { data = message };

            var json = JsonSerializer.Serialize(notificationMessage);
            var body = Encoding.UTF8.GetBytes(json);

            var props = channel.CreateBasicProperties();
            props.Persistent = true;

            channel.BasicPublish(
                exchange: "",
                routingKey: queueName,
                basicProperties: props,
                body: body);

            return Task.CompletedTask;
        }
    }
}
