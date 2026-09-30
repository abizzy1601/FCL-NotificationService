using NotificationService.Application.Abstractions.Messaging;
using NotificationService.Application.BasicResults;
using NotificationService.Application.Services;

namespace NotificationService.Application.UseCases.Command.MarkAllAsRead
{
    public record MarkAllAsReadCommand(string UserId) : ICommand<int>;

    public class MarkAllAsReadHandler : ICommandHandler<MarkAllAsReadCommand, int>
    {
        private readonly INotificationRepository _repository;
        private readonly INotificationCacheService _cache;

        public MarkAllAsReadHandler(INotificationRepository repository, INotificationCacheService cache)
        {
            _repository = repository;
            _cache = cache;
        }

        public async Task<Result<int>> Handle(MarkAllAsReadCommand command, CancellationToken ct)
        {
            var updated = await _repository.MarkAllAsReadAsync(command.UserId, ct);
            await _cache.SetUnreadCountAsync(command.UserId, 0, ct);
            return Result.Success(updated);
        }
    }
}
