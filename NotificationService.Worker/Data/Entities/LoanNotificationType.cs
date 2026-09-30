namespace NotificationService.Worker.Data.Entities
{
    public enum LoanNotificationType
    {
        AdvanceNotice,    // 3 days before the monthly due date
        DueDateReminder   // on the monthly due date itself
    }
}
