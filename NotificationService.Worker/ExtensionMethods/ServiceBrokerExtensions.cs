using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NotificationService.Worker.Data;
using NotificationService.Worker.ServiceBroker;
using NotificationService.Worker.Services;
using NotificationService.Worker.Settings;
using RabbitMQ.Client;

namespace NotificationService.Worker.ExtensionMethods
{
    public static class ServiceBrokerExtensions
    {
        public static IServiceCollection AddServiceBrokerListener(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ITransactionProcessor, TransactionProcessor>();
            services.AddHostedService<ServiceBrokerTransactionListener>();
            services.AddScoped<INotificationService, Services.NotificationService>();
            services.Configure<DaprSettings>(configuration.GetSection(DaprSettings.CONFIG_SECTION));
            services.Configure<RabbitMQSettings>(configuration.GetSection(RabbitMQSettings.CONFIG_SECTION));

            services.AddScoped<IRabbitMqPublisher, RabbitMqPublisher>();

            return services;
        }

        public static IServiceCollection ConfigureDb(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            return services;
        }

        public static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration config)
        {
            var rabbitMQConfig = config.GetSection(RabbitMQSettings.CONFIG_SECTION).Get<RabbitMQSettings>();

            var factory = new ConnectionFactory
            {
                HostName = rabbitMQConfig.Host,
                UserName = rabbitMQConfig.User,
                Password = rabbitMQConfig.Password,
                Port = rabbitMQConfig.Port,
                DispatchConsumersAsync = true
            };

            services.AddSingleton<IConnection>(_ => factory.CreateConnection());

            services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

            return services;
        }
    }
}
