using System.Text.Json;

namespace NotificationService.Worker.Middlewares
{
    internal sealed class ExceptionHandlingMiddleware : IMiddleware
    {
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                _logger.LogInformation($"\nMessage: {JsonSerializer.Serialize(e.Message)}\n Stack Trace: {JsonSerializer.Serialize(e.StackTrace)}\n");
            }
        }
    }
}
