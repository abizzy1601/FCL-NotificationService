using System.Net;
using System.Text.Json;

namespace NotificationService.API.Middlewares
{
    /// <summary>Catches anything that escapes a controller action and returns a consistent ProblemDetails-shaped JSON body instead of a raw 500 HTML page.</summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized request to {Path}", context.Request.Path);
                await WriteProblemAsync(context, HttpStatusCode.Unauthorized, "Unauthorized", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception on {Path}", context.Request.Path);
                await WriteProblemAsync(context, HttpStatusCode.InternalServerError, "An unexpected error occurred.", null);
            }
        }

        private static Task WriteProblemAsync(HttpContext context, HttpStatusCode statusCode, string title, string? detail)
        {
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)statusCode;

            var problem = new
            {
                type = $"https://httpstatuses.io/{(int)statusCode}",
                title,
                status = (int)statusCode,
                detail,
                traceId = context.TraceIdentifier
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}
