namespace NotificationService.Worker.Settings
{
    public class RabbitMQSettings
    {
        public const string CONFIG_SECTION = "RabbitMQ";
        public string Host { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
    }
}
