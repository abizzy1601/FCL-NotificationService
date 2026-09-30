using Microsoft.Extensions.Options;
using NotificationService.Application.Abstractions.Messaging;
using NotificationService.Application.BasicResults;
using NotificationService.Application.Config;
using NotificationService.Application.DTOs;
using NotificationService.Application.Services;

namespace NotificationService.Application.UseCases.Queries.GetUserNotifications
{
    public record GetUserNotificationsQuery(
        string UserId,
        int Page = 1,
        int? PageSize = null,
        bool? UnreadOnly = null) : IQuery<PagedResult<NotificationDto>>;

    public class GetUserNotificationsHandler
        : IQueryHandler<GetUserNotificationsQuery, PagedResult<NotificationDto>>
    {
        private readonly INotificationRepository _repository;
        private readonly NotificationSettings _settings;

        public GetUserNotificationsHandler(INotificationRepository repository, IOptions<NotificationSettings> settings)
        {
            _repository = repository;
            _settings = settings.Value;
        }

        public async Task<Result<PagedResult<NotificationDto>>> Handle(
            GetUserNotificationsQuery query, CancellationToken ct)
        {
            var page = query.Page < 1 ? 1 : query.Page;
            var pageSize = query.PageSize is null or < 1 or > int.MaxValue
                ? _settings.DefaultPageSize
                : Math.Min(query.PageSize.Value, _settings.MaxPageSize);

            var (items, totalCount) = await _repository.GetForUserAsync(
                query.UserId, page, pageSize, query.UnreadOnly, ct);

            var dtos = items.Select(n => new NotificationDto
            {
                Id = n.Id,
                Category = n.Category,
                Channel = n.Channel,
                Priority = n.Priority,
                Title = n.Title,
                Body = n.Body,
                ActionUrl = n.ActionUrl,
                Metadata = n.Metadata,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            }).ToList();

            return Result.Success(new PagedResult<NotificationDto>
            {
                Items = dtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }
    }
}
