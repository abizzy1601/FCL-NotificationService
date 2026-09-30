namespace NotificationService.Worker.Settings
{
    public class LoanReminderSettings
    {
        public const string CONFIG_SECTION = "LoanReminder";

        public bool IsEnabled { get; set; }
        public int ScheduledHourUtc { get; set; } = 6;
        public int ScheduledMinuteUtc { get; set; } = 0;
        public int BatchSize { get; set; } = 100;
        public string NotificationTopic { get; set; } = "notification-service-notification";
        public List<string> TestReminderContacts { get; set; }
        public List<string> LoanProducts { get; set; }
        public string DueDateLoanReminderTemplate { get; set; }
        public string AdvanceLoanReminderTemplate { get; set; }

        public string SourceTableName { get; set; }
    }
}
