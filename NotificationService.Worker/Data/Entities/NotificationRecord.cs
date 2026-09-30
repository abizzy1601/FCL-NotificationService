using FastCredit.OmniChannel.Shared.AggregateBase;

namespace NotificationService.Worker.Data.Entities
{
    public class NotificationRecord : BaseEntity
    {
        public string UserId { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public bool IsBilled { get; set; }
        public NotificationType NotificationType { get; set; }
        public NotificationChannel NotificationChannel { get; set; }

        public User User { get; set; } = default!;
    }

    public enum NotificationType
    {
        None,
        FundTransfer
    }

    public enum NotificationChannel
    {
        Sms,
        Email
    }
}
