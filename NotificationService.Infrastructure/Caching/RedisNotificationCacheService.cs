using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Config;
using NotificationService.Application.Services;
using StackExchange.Redis;

namespace NotificationService.Infrastructure.Caching
{
    public class RedisNotificationCacheService : INotificationCacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisNotificationCacheService> _logger;
        private readonly TimeSpan _unreadCountTtl;

        public RedisNotificationCacheService(
            IConnectionMultiplexer redis,
            IOptions<NotificationSettings> settings,
            ILogger<RedisNotificationCacheService> logger)
        {
            _redis = redis;
            _logger = logger;
            _unreadCountTtl = TimeSpan.FromDays(settings.Value.UnreadCountCacheTtlDays);
        }

        private IDatabase Db => _redis.GetDatabase();

        private static string UnreadKey(string userId) => $"notif:unread:{userId}";

        private static string DailyCategoryKey(string userId, string categoryKey) =>
            $"notif:daily:{categoryKey}:{userId}:{DateTime.UtcNow:yyyyMMdd}";

        public async Task<int?> GetUnreadCountAsync(string userId, CancellationToken ct)
        {
            try
            {
                var value = await Db.StringGetAsync(UnreadKey(userId));
                return value.HasValue ? (int)value : null;
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Redis unavailable reading unread count for {UserId}", userId);
                return null; // caller falls back to SQL
            }
        }

        public Task SetUnreadCountAsync(string userId, int count, CancellationToken ct) =>
            Db.StringSetAsync(UnreadKey(userId), count, _unreadCountTtl);

        public async Task<int> IncrementUnreadCountAsync(string userId, CancellationToken ct)
        {
            var newValue = await Db.StringIncrementAsync(UnreadKey(userId));
            await Db.KeyExpireAsync(UnreadKey(userId), _unreadCountTtl);
            return (int)newValue;
        }

        public async Task<int> DecrementUnreadCountAsync(string userId, int by, CancellationToken ct)
        {
            var newValue = await Db.StringDecrementAsync(UnreadKey(userId), by);
            if (newValue < 0)
            {
                await Db.StringSetAsync(UnreadKey(userId), 0, _unreadCountTtl);
                return 0;
            }
            return (int)newValue;
        }

        public async Task<int> IncrementDailyCategoryCounterAsync(string userId, string categoryKey, CancellationToken ct)
        {
            var key = DailyCategoryKey(userId, categoryKey);
            var newValue = await Db.StringIncrementAsync(key);
            if (newValue == 1)
                await Db.KeyExpireAsync(key, TimeSpan.FromHours(25));
            return (int)newValue;
        }
    }
}
