using NotificationService.Core.Enums;

namespace NotificationService.Core.Events
{
    public class NotificationEvent
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString();

        public string? UserId { get; set; }

        public NotificationCategory Category { get; set; }

        public NotificationChannel Channel { get; set; } = NotificationChannel.InApp;

        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

        public string Title { get; set; } = default!;

        public string Body { get; set; } = default!;

        public Dictionary<string, object?>? Metadata { get; set; }

        public string? ActionUrl { get; set; }

        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}
