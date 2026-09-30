using Microsoft.OpenApi.Models;

namespace NotificationService.API.Extensions
{
    /// <summary>
    /// Registers the v1 Swagger document. Endpoint versioning here is by URL
    /// segment (api/v1/...) rather than the Asp.Versioning package, so this stays
    /// a single static doc for now — add one AddSwaggerDoc call per version if a
    /// v2 controller is introduced later.
    /// </summary>
    public static class ConfigureSwaggerOptions
    {
        public static IServiceCollection AddVersionedSwaggerGen(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Notification Service API",
                    Version = "v1",
                    Description = "In-app and push notifications."
                });
            });

            return services;
        }
    }
}
