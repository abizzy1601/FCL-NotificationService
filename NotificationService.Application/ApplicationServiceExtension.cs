using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Abstractions.Messaging;

namespace NotificationService.Application
{
    public static class ApplicationServiceExtension
    {
        public static IServiceCollection AddApplicationService(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(ApplicationAssemblyReference.Assembly));

            services.AddValidatorsFromAssembly(ApplicationAssemblyReference.Assembly);

            // Order matters: validate first, throttle second, log last (wraps the actual handler).
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

            return services;
        }
    }
}
