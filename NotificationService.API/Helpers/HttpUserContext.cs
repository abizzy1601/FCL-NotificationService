using NotificationService.Application;

namespace NotificationService.API.Helpers
{
    public class HttpUserContext : IUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpUserContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string UserId =>
            _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("CustomerId")?.Value
            ?? _httpContextAccessor.HttpContext?.Request.Headers["X-User-Id"].FirstOrDefault()
            ?? throw new UnauthorizedAccessException("No authenticated user found on the current request.");
    }
}
