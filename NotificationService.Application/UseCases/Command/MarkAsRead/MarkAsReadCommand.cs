using NotificationService.Application.Abstractions.Messaging;
using NotificationService.Application.BasicResults;
using NotificationService.Application.Services;

namespace NotificationService.Application.UseCases.Command.MarkAsRead
{
    public record MarkAsReadCommand(Guid NotificationId, string UserId) : ICommand;

    public class MarkAsReadHandler : ICommandHandler<MarkAsReadCommand>
    {
        private readonly INotificationRepository _repository;
        private readonly INotificationCacheService _cache;

        public MarkAsReadHandler(INotificationRepository repository, INotificationCacheService cache)
        {
            _repository = repository;
            _cache = cache;
        }

        public async Task<Result> Handle(MarkAsReadCommand command, CancellationToken ct)
        {
            var notification = await _repository.GetOwnedByIdAsync(command.NotificationId, command.UserId, ct);
            if (notification is null)
                return Result.Failure(Error.NotFound("Notification not found."));

            if (notification.IsRead)
                return Result.Success();

            await _repository.MarkAsReadAsync(command.NotificationId, command.UserId, ct);
            await _cache.DecrementUnreadCountAsync(command.UserId, 1, ct);

            return Result.Success();
        }
    }
}
