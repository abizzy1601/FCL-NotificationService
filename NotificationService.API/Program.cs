using FastCredit.OmniChannel.Shared.Configurators;
using NotificationService.API.Extensions;
using NotificationService.API.Helpers;
using NotificationService.API.Middlewares;
using NotificationService.Application;
using NotificationService.Application.Config;
using NotificationService.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var serviceName = builder.Configuration.GetValue<string>("SERVICE_NAME") ?? "notification-service-api";
builder.Logging.ClearProviders();
var logger = Log.Logger = LoggerConfigurator.ConfigureLogger(builder.Configuration, serviceName);
builder.Host.UseSerilog(logger);

// Mirrors the Worker's Config-folder pattern: appsettings.json plus every json
// file under Config/ gets merged in, so environment-specific values (rabbitmq,
// redis, notification limits) can be split into their own files if desired.
builder.Host.ConfigureAppConfiguration((hostingContext, config) =>
{
    config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    var configFiles = hostingContext.HostingEnvironment.ContentRootFileProvider.GetDirectoryContents("Config");
    if (configFiles.Exists)
        configFiles.ToList().ForEach(file => config.AddJsonFile($"Config/{file.Name}", optional: true, reloadOnChange: true));
});

builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = CustomInvalidModelStateResponseFactory.Create;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddVersionedSwaggerGen();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, HttpUserContext>();

builder.Services.AddApplicationService();
builder.Services.AddInfrastructureService(builder.Configuration);
builder.Services.AddNotificationConsumer();

builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var corsSettings = builder.Configuration.GetSection(CorsSettings.CONFIG_SECTION).Get<CorsSettings>()
            ?? new CorsSettings();

        policy.WithOrigins(corsSettings.AllowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // required for SignalR
    });
});

var app = builder.Build();

app.UseExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Notification Service API v1"));
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();
