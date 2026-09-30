
using FastCredit.OmniChannel.Shared.Configurators;
using NotificationService.Worker.ExtensionMethods;
using Serilog;

namespace NotificationService.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var serviceName = builder.Configuration.GetValue<string>("SERVICE_NAME") ?? "default-service";
            builder.Logging.ClearProviders();
            var logger = Log.Logger = LoggerConfigurator.ConfigureLogger(builder.Configuration, serviceName);
            builder.Host.UseSerilog(logger);

            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Host.ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                var configFiles = hostingContext.HostingEnvironment.ContentRootFileProvider.GetDirectoryContents("Config");
                configFiles.ToList().ForEach(file => config.AddJsonFile($"Config/{file.Name}", optional: false, reloadOnChange: true));
            });

            builder.Services.AddServiceBrokerListener(builder.Configuration);
            builder.Services.ConfigureDb(builder.Configuration);
            builder.Services.AddRabbitMq(builder.Configuration);
            builder.Services.AddLoanReminderJob(builder.Configuration);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
