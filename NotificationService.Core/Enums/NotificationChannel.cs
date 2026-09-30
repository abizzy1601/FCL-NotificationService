namespace NotificationService.Core.Enums
{
    /// <summary>
    /// Which surface(s) a notification is delivered to. [Flags] so a single row
    /// can target InApp + Push together. Sms/Email are listed but not yet acted
    /// on here — the Worker still owns that flow; this leaves room to fold them
    /// in later (e.g. via a Channel-aware dispatcher) without another schema change.
    /// </summary>
    [Flags]
    public enum NotificationChannel
    {
        InApp = 1,
        Push = 2,
        Sms = 4,
        Email = 8
    }
}
