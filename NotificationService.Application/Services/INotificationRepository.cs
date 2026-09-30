using NotificationService.Core.Entities;

namespace NotificationService.Application.Services
{
    public interface INotificationRepository
    {
        Task<bool> ExistsByEventIdAsync(string eventId, CancellationToken ct);

        Task AddAsync(Notification notification, CancellationToken ct);

        Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetForUserAsync(
            string userId, int page, int pageSize, bool? unreadOnly, CancellationToken ct);
        Task<Notification?> GetOwnedByIdAsync(Guid id, string userId, CancellationToken ct);

        Task<int> GetUnreadCountFromDbAsync(string userId, CancellationToken ct);

        Task MarkAsReadAsync(Guid notificationId, string userId, CancellationToken ct);

        Task<int> MarkAllAsReadAsync(string userId, CancellationToken ct);
    }
}
