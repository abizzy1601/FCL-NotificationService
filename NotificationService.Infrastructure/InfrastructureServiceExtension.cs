using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Config;
using NotificationService.Application.Services;
using NotificationService.Infrastructure.Caching;
using NotificationService.Infrastructure.Messaging.RabbitMq;
using NotificationService.Infrastructure.Persistence;
using NotificationService.Infrastructure.Persistence.Repositories;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace NotificationService.Infrastructure
{
    public static class InfrastructureServiceExtension
    {
        public static IServiceCollection AddInfrastructureService(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RabbitMqSettings>(configuration.GetSection(RabbitMqSettings.CONFIG_SECTION));
            services.Configure<RedisSettings>(configuration.GetSection(RedisSettings.CONFIG_SECTION));
            services.Configure<NotificationSettings>(configuration.GetSection(NotificationSettings.CONFIG_SECTION));
            services.Configure<CorsSettings>(configuration.GetSection(CorsSettings.CONFIG_SECTION));

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddSingleton<IConnection>(sp =>
            {
                var settings = configuration.GetSection(RabbitMqSettings.CONFIG_SECTION).Get<RabbitMqSettings>()
                    ?? throw new InvalidOperationException($"'{RabbitMqSettings.CONFIG_SECTION}' configuration section is missing.");

                var factory = new ConnectionFactory
                {
                    HostName = settings.Host,
                    UserName = settings.User,
                    Password = settings.Password,
                    Port = settings.Port,
                    DispatchConsumersAsync = true
                };

                return factory.CreateConnection("notification-service");
            });

            services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RedisSettings>>().Value;
                if (string.IsNullOrWhiteSpace(settings.ConnectionString))
                    throw new InvalidOperationException(
                        $"'{RedisSettings.CONFIG_SECTION}:ConnectionString' is not configured.");

                return ConnectionMultiplexer.Connect(settings.ConnectionString);
            });

            services.AddScoped<INotificationCacheService, RedisNotificationCacheService>();
            services.AddScoped<INotificationRepository, NotificationRepository>();

            return services;
        }

        /// <summary>Adds the background consumer that turns RabbitMQ messages into notifications.</summary>
        public static IServiceCollection AddNotificationConsumer(this IServiceCollection services)
        {
            services.AddHostedService<NotificationConsumer>();
            return services;
        }
    }
}
