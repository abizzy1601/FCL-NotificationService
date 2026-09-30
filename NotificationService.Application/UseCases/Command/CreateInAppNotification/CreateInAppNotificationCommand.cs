using Microsoft.Extensions.Logging;
using NotificationService.Application.Abstractions.Messaging;
using NotificationService.Application.BasicResults;
using NotificationService.Application.DTOs;
using NotificationService.Application.Services;
using NotificationService.Core.Entities;
using NotificationService.Core.Enums;

namespace NotificationService.Application.UseCases.Command.CreateNotification
{
    public record CreateNotificationCommand(
        string EventId,
        string? UserId,
        NotificationCategory Category,
        NotificationChannel Channel,
        NotificationPriority Priority,
        string Title,
        string Body,
        Dictionary<string, object?>? Metadata,
        string? ActionUrl,
        DateTime? ExpiresAt = null) : ICommand<Guid>;

    public class CreateNotificationHandler : ICommandHandler<CreateNotificationCommand, Guid>
    {
        private readonly INotificationRepository _repository;
        private readonly INotificationCacheService _cache;
        private readonly ILogger<CreateNotificationHandler> _logger;

        public CreateNotificationHandler(
            INotificationRepository repository,
            INotificationCacheService cache,
            ILogger<CreateNotificationHandler> logger)
        {
            _repository = repository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(CreateNotificationCommand command, CancellationToken ct)
        {
            // Idempotency: a redelivered message must never create a duplicate row.
            if (await _repository.ExistsByEventIdAsync(command.EventId, ct))
            {
                _logger.LogInformation(
                    "CreateNotification: EventId={EventId} already processed — skipping", command.EventId);
                return Result.Failure<Guid>(Error.Conflict("Notification already processed for this EventId."));
            }

            var notification = new Notification
            {
                EventId = command.EventId,
                UserId = command.UserId,
                Category = command.Category,
                Channel = command.Channel,
                Priority = command.Priority,
                Title = command.Title,
                Body = command.Body,
                Metadata = command.Metadata,
                ActionUrl = command.ActionUrl,
                ExpiresAt = command.ExpiresAt,
                IsRead = false
            };

            await _repository.AddAsync(notification, ct);

            var dto = new NotificationDto
            {
                Id = notification.Id,
                Category = notification.Category,
                Channel = notification.Channel,
                Priority = notification.Priority,
                Title = notification.Title,
                Body = notification.Body,
                ActionUrl = notification.ActionUrl,
                Metadata = notification.Metadata,
                IsRead = false,
                CreatedAt = notification.CreatedAt
            };

            await _cache.IncrementUnreadCountAsync(notification.UserId, ct);

            return Result.Success(notification.Id);
        }
    }
}
