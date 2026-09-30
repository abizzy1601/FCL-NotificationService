using NotificationService.Core.Enums;

namespace NotificationService.Application.DTOs
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public NotificationCategory Category { get; set; }
        public NotificationChannel Channel { get; set; }
        public NotificationPriority Priority { get; set; }
        public string Title { get; set; } = default!;
        public string Body { get; set; } = default!;
        public string? ActionUrl { get; set; }
        public Dictionary<string, object?>? Metadata { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
