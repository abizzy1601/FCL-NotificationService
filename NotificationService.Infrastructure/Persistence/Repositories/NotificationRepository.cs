using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Services;
using NotificationService.Core.Entities;

namespace NotificationService.Infrastructure.Persistence.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly ApplicationDbContext _context;

        public NotificationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<bool> ExistsByEventIdAsync(string eventId, CancellationToken ct) =>
            _context.Notifications.AnyAsync(n => n.EventId == eventId, ct);

        public async Task AddAsync(Notification notification, CancellationToken ct)
        {
            await _context.Notifications.AddAsync(notification, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetForUserAsync(
            string userId, int page, int pageSize, bool? unreadOnly, CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            var query = _context.Notifications.AsNoTracking().Where(n =>
                n.UserId == userId ||
                (n.UserId == null && (n.ExpiresAt == null || n.ExpiresAt > now)));

            if (unreadOnly == true)
                query = query.Where(n => n.UserId == null || !n.IsRead);

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public Task<Notification?> GetOwnedByIdAsync(Guid id, string userId, CancellationToken ct) =>
            _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, ct);

        public Task<int> GetUnreadCountFromDbAsync(string userId, CancellationToken ct) =>
            _context.Notifications.Where(n => n.UserId == userId && !n.IsRead).CountAsync(ct);

        public async Task MarkAsReadAsync(Guid notificationId, string userId, CancellationToken ct)
        {
            await _context.Notifications
                .Where(n => n.Id == notificationId && n.UserId == userId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);
        }

        public async Task<int> MarkAllAsReadAsync(string userId, CancellationToken ct) =>
            await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);
    }
}
