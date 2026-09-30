using MediatR;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application;
using NotificationService.Application.UseCases.Command.MarkAllAsRead;
using NotificationService.Application.UseCases.Command.MarkAsRead;
using NotificationService.Application.UseCases.Queries.GetUnreadCount;
using NotificationService.Application.UseCases.Queries.GetUserNotifications;
using NotificationService.Core.Enums;

namespace NotificationService.API.Controllers.V1
{
    [ApiController]
    [Route("api/v1/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly ISender _mediator;
        private readonly IUserContext _userContext;

        public NotificationsController(ISender mediator, IUserContext userContext)
        {
            _mediator = mediator;
            _userContext = userContext;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int? pageSize = null, [FromQuery] bool? unreadOnly = null, CancellationToken ct = default)
        {
            var result = await _mediator.Send(new GetUserNotificationsQuery(_userContext.UserId, page, pageSize, unreadOnly), ct);

            return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
        {
            var result = await _mediator.Send(new GetUnreadCountQuery(_userContext.UserId), ct);
            return result.IsSuccess ? Ok(new { unreadCount = result.Value }) : Problem(result.Error.Message);
        }

        [HttpPost("{id:guid}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
        {
            var result = await _mediator.Send(new MarkAsReadCommand(id, _userContext.UserId), ct);

            if (result.IsSuccess) return NoContent();
            return result.Error.Code == "NotFound" ? NotFound() : Problem(result.Error.Message);
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
        {
            var result = await _mediator.Send(new MarkAllAsReadCommand(_userContext.UserId), ct);
            return result.IsSuccess ? Ok(new { updated = result.Value }) : Problem(result.Error.Message);
        }
    }
}
