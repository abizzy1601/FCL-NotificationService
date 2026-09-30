using Microsoft.EntityFrameworkCore;
using NotificationService.Worker.Data;
using NotificationService.Worker.Jobs;
using NotificationService.Worker.Services;
using NotificationService.Worker.Settings;

namespace NotificationService.Worker.ExtensionMethods
{
    public static class LoanReminderExtensions
    {
        public static IServiceCollection AddLoanReminderJob(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<LoanReminderSettings>(
                configuration.GetSection(LoanReminderSettings.CONFIG_SECTION));

            services.AddDbContext<PARLoanDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("PARLoanConnection")));

            var settings = configuration
                            .GetSection(LoanReminderSettings.CONFIG_SECTION)
                            .Get<LoanReminderSettings>() ?? new LoanReminderSettings();

            if (settings.IsEnabled)
            {
                services.AddScoped<ILoanReminderService, LoanReminderService>();
                services.AddHostedService<LoanRepaymentReminderJob>();
            }

            return services;
        }
    }
}
