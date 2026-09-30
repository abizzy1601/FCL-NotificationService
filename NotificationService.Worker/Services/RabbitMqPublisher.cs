using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace NotificationService.Worker.Services
{
    public interface IRabbitMqPublisher
    {
        Task PublishAsync<T>(string queueName, T message);
    }

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