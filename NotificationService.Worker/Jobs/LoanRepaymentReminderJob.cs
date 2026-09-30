using Microsoft.Extensions.Options;
using NotificationService.Worker.Services;
using NotificationService.Worker.Settings;

namespace NotificationService.Worker.Jobs
{
    public class LoanRepaymentReminderJob : BackgroundService
    {
        private readonly ILogger<LoanRepaymentReminderJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly LoanReminderSettings _settings;

        public LoanRepaymentReminderJob(
            ILogger<LoanRepaymentReminderJob> logger,
            IServiceScopeFactory scopeFactory,
            IOptions<LoanReminderSettings> settings)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _settings = settings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LoanRepaymentReminderJob started — scheduled daily at {Hour:D2}:{Minute:D2} UTC",
                _settings.ScheduledHourUtc, _settings.ScheduledMinuteUtc);

            while (!stoppingToken.IsCancellationRequested)
            {
                var utcNow = DateTime.UtcNow;
                var nextRun = GetNextRunTime(utcNow);
                var delay = nextRun - utcNow;

                _logger.LogInformation(
                    "LoanRepaymentReminderJob: next run at {NextRun:yyyy-MM-dd HH:mm} UTC (in {Hours}h {Minutes}m)",
                    nextRun, (int)delay.TotalHours, delay.Minutes);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (stoppingToken.IsCancellationRequested) break;

                _logger.LogInformation("LoanRepaymentReminderJob: firing at {Now:yyyy-MM-dd HH:mm} UTC", DateTime.UtcNow);

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<ILoanReminderService>();
                    await service.ProcessDailyRemindersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "LoanRepaymentReminderJob: unhandled error during run — will retry tomorrow");
                }
            }

            _logger.LogInformation("LoanRepaymentReminderJob stopped");
        }

        private DateTime GetNextRunTime(DateTime utcNow)
        {
            var todayRun = new DateTime(
                utcNow.Year, utcNow.Month, utcNow.Day,
                _settings.ScheduledHourUtc, _settings.ScheduledMinuteUtc, 0,
                DateTimeKind.Utc);

            return utcNow < todayRun ? todayRun : todayRun.AddDays(1);
        }
    }
}
