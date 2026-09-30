using NotificationService.Application.Abstractions.Messaging;
using NotificationService.Application.BasicResults;
using NotificationService.Application.Services;

namespace NotificationService.Application.UseCases.Queries.GetUnreadCount
{
    public record GetUnreadCountQuery(string UserId) : IQuery<int>;

    public class GetUnreadCountHandler : IQueryHandler<GetUnreadCountQuery, int>
    {
        private readonly INotificationCacheService _cache;
        private readonly INotificationRepository _repository;

        public GetUnreadCountHandler(INotificationCacheService cache, INotificationRepository repository)
        {
            _cache = cache;
            _repository = repository;
        }

        public async Task<Result<int>> Handle(GetUnreadCountQuery query, CancellationToken ct)
        {
            var cached = await _cache.GetUnreadCountAsync(query.UserId, ct);
            if (cached.HasValue)
                return Result.Success(cached.Value);

            var actual = await _repository.GetUnreadCountFromDbAsync(query.UserId, ct);
            await _cache.SetUnreadCountAsync(query.UserId, actual, ct);

            return Result.Success(actual);
        }
    }
}
