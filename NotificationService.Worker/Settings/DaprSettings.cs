namespace NotificationService.Worker.Settings
{
    public class DaprSettings
    {
        public const string CONFIG_SECTION = "dapr";

        public string NotificationTopic { get; set; }

        public string PubSub { get; set; }

        public string FundTransfer { get; set; }
    }
}
