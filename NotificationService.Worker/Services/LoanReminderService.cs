using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NotificationService.Worker.Data;
using NotificationService.Worker.Data.Entities;
using NotificationService.Worker.DTOs;
using NotificationService.Worker.ExtensionMethods;
using NotificationService.Worker.Settings;

namespace NotificationService.Worker.Services
{
    public interface ILoanReminderService
    {
        Task ProcessDailyRemindersAsync(CancellationToken cancellationToken);
    }

    public class LoanReminderService : ILoanReminderService
    {
        private readonly ILogger<LoanReminderService> _logger;
        private readonly PARLoanDbContext _loanContext;
        private readonly IRabbitMqPublisher _rabbitMqPublisher;
        private readonly LoanReminderSettings _settings;
        private readonly IHostEnvironment _env;

        public LoanReminderService(
            ILogger<LoanReminderService> logger,
            PARLoanDbContext loanContext,
            IRabbitMqPublisher rabbitMqPublisher,
            IOptions<LoanReminderSettings> settings,
            IHostEnvironment env)
        {
            _logger = logger;
            _loanContext = loanContext;
            _rabbitMqPublisher = rabbitMqPublisher;
            _settings = settings.Value;
            _env = env;
        }

        public async Task ProcessDailyRemindersAsync(CancellationToken cancellationToken)
        {
            var today = DateTime.UtcNow.Date;
            _logger.LogInformation("LoanReminderService: starting run for {Date}", today);

            int cycleYear = today.Year;
            int cycleMonth = today.Month;
            var stats = new RunStats();

            await ProcessTriggerBatchedAsync(today, cycleYear, cycleMonth,
                LoanNotificationType.AdvanceNotice, today.AddDays(3).Day, stats, cancellationToken);

            await ProcessTriggerBatchedAsync(today, cycleYear, cycleMonth,
                LoanNotificationType.DueDateReminder, today.Day, stats, cancellationToken);

            _logger.LogInformation(
                "LoanReminderService: run complete — fetched={Fetched}, published={Published}, skipped={Skipped}, failed={Failed}",
                stats.Fetched, stats.Published, stats.Skipped, stats.Failed);
        }


        private async Task ProcessTriggerBatchedAsync(
            DateTime today, int cycleYear, int cycleMonth,
            LoanNotificationType triggerType, int targetDay,
            RunStats stats, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "LoanReminderService: starting {TriggerType} pass (disbursementDay={TargetDay})",
                triggerType, targetDay);

            int lastId = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                List<LoanAccount> batch;
                try
                {
                    batch = await FetchBatchAsync(today, targetDay, lastId, cancellationToken);
                }
                catch (DbException ex)
                {
                    _logger.LogError(ex,
                        "LoanReminderService: DB error fetching {TriggerType} batch after Id={LastId} — aborting this pass",
                        triggerType, lastId);
                    break;
                }

                if (batch.Count == 0) break;
                stats.Fetched += batch.Count;

                _logger.LogInformation(
                    "LoanReminderService: {TriggerType} — processing {Count} loan(s) (Id > {LastId})",
                    triggerType, batch.Count, lastId);

                foreach (var loan in batch)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        var result = await ProcessSingleLoanAsync(
                            loan, cycleYear, cycleMonth, triggerType, today, cancellationToken);

                        if (result == ProcessResult.Published) stats.Published++;
                        else if (result == ProcessResult.Skipped) stats.Skipped++;
                        else stats.Failed++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "LoanReminderService: unhandled error for LoanId={LoanId} [{TriggerType}]",
                            loan.Id, triggerType);
                        stats.Failed++;
                    }
                }

                // Partial batch means we've reached the end of eligible records
                if (batch.Count < _settings.BatchSize) break;

                // Advance the keyset cursor to avoid re-fetching processed records
                lastId = batch[^1].Id;
            }
        }

        private async Task<ProcessResult> ProcessSingleLoanAsync(
            LoanAccount loan, int cycleYear, int cycleMonth,
            LoanNotificationType notificationType, DateTime today, CancellationToken cancellationToken)
        {
            bool alreadySent = await _loanContext.LoanReminderRecords
                .AnyAsync(r => r.LoanId == loan.Id
                            && r.CycleYear == cycleYear
                            && r.CycleMonth == cycleMonth
                            && r.NotificationType == notificationType
                            && r.IsSuccess,
                           cancellationToken);

            if (alreadySent)
            {
                _logger.LogDebug(
                    "LoanReminderService: LoanId={LoanId} already sent {NotificationType} for {Year}-{Month:D2} — skipping",
                    loan.Id, notificationType, cycleYear, cycleMonth);
                return ProcessResult.Skipped;
            }

            string normalizedPhone;
            try
            {
                normalizedPhone = loan.PhoneNo.ExtractAndNormalizePhone();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(
                    "LoanReminderService: invalid phone for LoanId={LoanId} (raw='{Phone}'): {Error}",
                    loan.Id, loan.PhoneNo, ex.Message);
                await WriteReminderRecordAsync(loan, cycleYear, cycleMonth, notificationType,
                    false, ex.Message, cancellationToken);
                return ProcessResult.Failed;
            }

            var dueDate = ComputeDueDate(loan.DisbursementDate, today);
            var message = BuildSmsMessage(loan, normalizedPhone, dueDate, notificationType);

            try
            {
                await _rabbitMqPublisher.PublishAsync<Message>(_settings.NotificationTopic, message);
                _logger.LogInformation(
                    "LoanReminderService: {NotificationType} SMS published for LoanId={LoanId}, Phone={Phone}, DueDate={DueDate}",
                    notificationType, loan.Id, normalizedPhone, dueDate.ToString("dd MMM yyyy"));
                await WriteReminderRecordAsync(loan, cycleYear, cycleMonth, notificationType,
                    true, null, cancellationToken);
                return ProcessResult.Published;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "LoanReminderService: publish failed for LoanId={LoanId} [{NotificationType}]",
                    loan.Id, notificationType);
                await WriteReminderRecordAsync(loan, cycleYear, cycleMonth, notificationType,
                    false, $"Publish failed: {ex.Message}", cancellationToken);
                return ProcessResult.Failed;
            }
        }

        private Message BuildSmsMessage(
            LoanAccount loan, string normalizedPhone, DateTime dueDate,
            LoanNotificationType notificationType)
        {
            // In dev, send to a random test contact instead of the real customer number
            var destination = _env.IsDevelopment() && _settings.TestReminderContacts?.Count > 0
                ? _settings.TestReminderContacts[new Random().Next(_settings.TestReminderContacts.Count)]
                : normalizedPhone;

            if (_env.IsDevelopment())
                _logger.LogInformation(
                    "LoanReminderService: dev mode — routing LoanId={LoanId} SMS to test contact {Contact}",
                    loan.Id, destination);

            return new Message
            {
                Type = "sms",
                TemplateName = notificationType == LoanNotificationType.AdvanceNotice ? _settings.AdvanceLoanReminderTemplate : _settings.DueDateLoanReminderTemplate,
                To = destination,
                Params = new
                {
                    customerName = loan.CustomerName.Split(",", StringSplitOptions.TrimEntries).First()
                }
            };
        }

        private static DateTime ComputeDueDate(DateTime disbursementDate, DateTime today)
        {
            int disbursementDay = disbursementDate.Day;
            int daysInCurrentMonth = DateTime.DaysInMonth(today.Year, today.Month);
            return new DateTime(today.Year, today.Month, Math.Min(disbursementDay, daysInCurrentMonth));
        }

        private async Task WriteReminderRecordAsync(
            LoanAccount loan, int cycleYear, int cycleMonth,
            LoanNotificationType notificationType, bool isSuccess, string? errorMessage,
            CancellationToken cancellationToken)
        {
            var record = new LoanReminderRecord
            {
                LoanId = loan.Id,
                Arrangement = loan.Arrangement,
                CycleYear = cycleYear,
                CycleMonth = cycleMonth,
                NotificationType = notificationType,
                PhoneNumber = loan.PhoneNo,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage
            };

            try
            {
                await _loanContext.LoanReminderRecords.AddAsync(record, cancellationToken);
                await _loanContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "LoanReminderService: failed to write reminder record for LoanId={LoanId} [{NotificationType}]",
                    loan.Id, notificationType);
            }
        }

        private Task<List<LoanAccount>> FetchBatchAsync(
            DateTime today, int targetDay, int lastId, CancellationToken cancellationToken)
        {
            var population = _loanContext.LoanAccounts
                .Where(l => l.MaturityDate > today
                         && l.DisbursementDate < today
                         && l.DisbursementDate.Day == targetDay
                         && _settings.LoanProducts.Contains(l.Product));

            return population
                .Where(l => l.Id > lastId
                         && !population.Any(other =>
                                other.PhoneNo == l.PhoneNo
                             && (other.ReportDate > l.ReportDate
                              || (other.ReportDate == l.ReportDate && other.Id > l.Id))))
                .OrderBy(l => l.Id)
                .Take(_settings.BatchSize)
                .ToListAsync(cancellationToken);
        }

        private enum ProcessResult { Published, Skipped, Failed }

        private sealed class RunStats
        {
            public int Fetched;
            public int Published;
            public int Skipped;
            public int Failed;
        }
    }
}
