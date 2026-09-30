namespace NotificationService.Core.Entities
{
    public class NotificationPreference
    {
        public string UserId { get; set; } = default!;

        public bool InAppEnabled { get; set; } = true;

        public bool PushEnabled { get; set; } = true;

        public bool PromotionsOptIn { get; set; } = true;

        /// <summary>JSON array of NotificationCategory values the user has muted.</summary>
        public string? MutedCategories { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
