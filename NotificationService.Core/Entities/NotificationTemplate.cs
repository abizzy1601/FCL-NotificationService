using NotificationService.Core.Enums;

namespace NotificationService.Core.Entities
{
    public class NotificationTemplate : Entity
    {
        public string Code { get; set; } = default!;
        public string TitleTemplate { get; set; } = default!;
        public string BodyTemplate { get; set; } = default!;
        public NotificationCategory Category { get; set; }
        public NotificationPriority DefaultPriority { get; set; } = NotificationPriority.Normal;
        public string Locale { get; set; } = "en";
        public bool IsActive { get; set; } = true;
    }
}
