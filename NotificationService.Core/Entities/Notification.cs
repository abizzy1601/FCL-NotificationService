using NotificationService.Core.Enums;

namespace NotificationService.Core.Entities
{
    public class Notification : Entity
    {
        public string? UserId { get; set; }

        public NotificationCategory Category { get; set; }

        public NotificationChannel Channel { get; set; } = NotificationChannel.InApp;

        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

        public string Title { get; set; } = default!;

        public string Body { get; set; } = default!;

        public Dictionary<string, object?>? Metadata { get; set; }

        public string? ActionUrl { get; set; }

        public bool IsRead { get; set; }

        public DateTime? ReadAt { get; set; }

        public DateTime? ExpiresAt { get; set; }
        public string EventId { get; set; } = default!;
    }
}
