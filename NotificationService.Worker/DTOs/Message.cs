using System.Text.Json.Serialization;

namespace NotificationService.Worker.DTOs
{
    public class Message
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("to")]
        public string To { get; set; }

        [JsonPropertyName("templateName")]
        public string TemplateName { get; set; }

        [JsonPropertyName("isInline")]
        public bool IsInline { get; set; }

        [JsonPropertyName("message")]
        public string MessageBody { get; set; }

        [JsonPropertyName("subject")]
        public string Subject { get; set; }

        [JsonPropertyName("params")]
        public object Params { get; set; }
    }
}
