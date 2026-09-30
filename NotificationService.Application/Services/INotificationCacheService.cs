namespace NotificationService.Application.Services
{
    public interface INotificationCacheService
    {
        Task<int?> GetUnreadCountAsync(string userId, CancellationToken ct);
        Task SetUnreadCountAsync(string userId, int count, CancellationToken ct);
        Task<int> IncrementUnreadCountAsync(string userId, CancellationToken ct);
        Task<int> DecrementUnreadCountAsync(string userId, int by, CancellationToken ct);

        /// <summary>Sliding daily counter keyed by category (e.g. NotificationConstants.PromoDailyCounterKey) — used by ThrottlingBehavior.</summary>
        Task<int> IncrementDailyCategoryCounterAsync(string userId, string categoryKey, CancellationToken ct);
    }
}
