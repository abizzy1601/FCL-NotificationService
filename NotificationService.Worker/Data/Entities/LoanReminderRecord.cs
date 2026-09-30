using FastCredit.OmniChannel.Shared.AggregateBase;

namespace NotificationService.Worker.Data.Entities
{
    public class LoanReminderRecord : BaseEntity
    {
        public long LoanId { get; set; }
        public string Arrangement { get; set; } = default!;
        public int CycleYear { get; set; }
        public int CycleMonth { get; set; }
        public LoanNotificationType NotificationType { get; set; }
        public string PhoneNumber { get; set; } = default!;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }
}