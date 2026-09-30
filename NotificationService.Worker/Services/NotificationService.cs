using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NotificationService.Worker.Data;
using NotificationService.Worker.Data.Entities;
using NotificationService.Worker.DTOs;
using NotificationService.Worker.Settings;
using NotificationService.Worker.Utils;

namespace NotificationService.Worker.Services
{
    public interface INotificationService
    {
        Task SendTransferSuccessNotificationAsync(TransactionNotification notification);
    }

    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IRabbitMqPublisher _rabbitMqPublisher;
        private readonly DaprSettings _daprSettings;

        public NotificationService(ILogger<NotificationService> logger,
            ApplicationDbContext dbContext,
            IWebHostEnvironment webHostEnvironment,
            IRabbitMqPublisher rabbitMqPublisher,
            IOptions<DaprSettings> daprSettings)
        {
            _logger = logger;
            _context = dbContext;
            _webHostEnvironment = webHostEnvironment;
            _rabbitMqPublisher = rabbitMqPublisher;
            _daprSettings = daprSettings.Value;
        }

        public async Task SendTransferSuccessNotificationAsync(TransactionNotification notification)
        {
            try
            {
                string dateFormatted = ConvertToStandardFormat(notification.PROCESSING_DATE, TimeOnly.FromDateTime(notification.SyncedAt));

                if (notification.TRANSACTION_TYPE == Constants.INTRABANK)
                {
                    var debitCustomerPref = await GetCustomerPrefAsync(notification.DEBIT_CUSTOMER_ID);
                    var creditCustomerPref = await GetCustomerPrefAsync(notification.CREDIT_CUSTOMER_ID);

                    if (debitCustomerPref != null)
                    {
                        await SendSmsAsync(debitCustomerPref, "Debit", notification.DEBIT_ACCT_NO, 
                            notification.DEBIT_AMOUNT, dateFormatted, notification.PAYMENT_DETAILS, notification.DEBIT_ACCOUNT_BALANCE);
                        await SendEmailAsync(debitCustomerPref, "Debit", notification.DEBIT_ACCT_NO, 
                            notification.DEBIT_AMOUNT, dateFormatted, notification.PAYMENT_DETAILS, notification.DEBIT_ACCOUNT_BALANCE, notification.SESSION_ID);
                    }
                    else
                    {
                        _logger.LogInformation($"Debit Customer preferences not found for customerID: {notification.DEBIT_CUSTOMER_ID}");
                    }

                    if (creditCustomerPref != null)
                    {
                        await SendSmsAsync(creditCustomerPref, "Credit", notification.CREDIT_ACCT_NO,
                            notification.CREDIT_AMOUNT, dateFormatted, notification.PAYMENT_DETAILS, notification.CREDIT_ACCOUNT_BALANCE);
                        await SendEmailAsync(creditCustomerPref, "Credit", notification.CREDIT_ACCT_NO,
                            notification.CREDIT_AMOUNT, dateFormatted, notification.PAYMENT_DETAILS, notification.CREDIT_ACCOUNT_BALANCE, notification.SESSION_ID);
                    }
                    else
                    {
                        _logger.LogInformation($"Credit Customer preferences not found for customerID: {notification.CREDIT_CUSTOMER_ID}");
                    }
                }
                else if (notification.TRANSACTION_TYPE == Constants.NIP_OUTWARD)
                {
                    var debitCustomerPref = await GetCustomerPrefAsync(notification.DEBIT_CUSTOMER_ID);
                    if (debitCustomerPref != null)
                    {
                        await SendSmsAsync(debitCustomerPref, "Debit", notification.DEBIT_ACCT_NO, 
                            notification.DEBIT_AMOUNT, dateFormatted, notification.PAYMENT_DETAILS, notification.DEBIT_ACCOUNT_BALANCE);
                        await SendEmailAsync(debitCustomerPref, "Debit", notification.DEBIT_ACCT_NO, 
                            notification.DEBIT_AMOUNT, dateFormatted, notification.PAYMENT_DETAILS, notification.DEBIT_ACCOUNT_BALANCE, notification.SESSION_ID);
                    }
                    else
                    {
                        _logger.LogInformation($"Debit Customer preferences not found for customerID: {notification.DEBIT_CUSTOMER_ID}");
                        return;
                    }
                }
                else if (notification.TRANSACTION_TYPE == Constants.NIP_INWARD)
                {
                    var creditCustomerPref = await GetCustomerPrefAsync(notification.CREDIT_CUSTOMER_ID);
                    if (creditCustomerPref != null)
                    {
                        await SendSmsAsync(creditCustomerPref, "Credit", notification.CREDIT_ACCT_NO, 
                            notification.CREDIT_AMOUNT, dateFormatted, notification.PAYMENT_DETAILS, notification.CREDIT_ACCOUNT_BALANCE);
                        await SendEmailAsync(creditCustomerPref, "Credit", notification.CREDIT_ACCT_NO, 
                            notification.CREDIT_AMOUNT, dateFormatted, notification.PAYMENT_DETAILS, notification.CREDIT_ACCOUNT_BALANCE, notification.SESSION_ID);
                    }
                    else
                    {
                        _logger.LogInformation($"Credit Customer preferences not found for customerID: {notification.CREDIT_CUSTOMER_ID}");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"An Exception occured while sending transfer notification: {ex.Message}");
                _logger.LogInformation(ex.StackTrace);
            }
        }

        private async Task<dynamic> GetCustomerPrefAsync(string customerId)
        {
            _logger.LogInformation($"Fetching customer preferences for user with CustomerId: {customerId}....");
            if (string.IsNullOrWhiteSpace(customerId))
            {
                _logger.LogInformation($"Customer ID was Null or Empty");
                return null;
            }
                
            return await (
                from u in _context.Users
                join p in _context.CustomerPreferences on u.Id equals p.UserId
                where u.CustomerId == customerId
                select new
                {
                    u.Id,
                    u.CustomerId,
                    u.Email,
                    u.Firstname,
                    u.Lastname,
                    u.Middlename,
                    u.PhoneNumber,
                    p.NotifyByMail,
                    p.NotifyBySms
                }).FirstOrDefaultAsync();
        }

        private async Task SendEmailAsync(dynamic customerPref, string transactionType, string accountNumber, decimal amount, string date, string narration, string newBalance, string sessionId)
        {
            if (customerPref?.NotifyByMail != true || string.IsNullOrWhiteSpace(customerPref.Email)) return;

            var template = await NotificationHelper.RetrieveTemplateAsync("fundtransfer.html", _webHostEnvironment.WebRootPath);
            if (!template.Successful) return;

            string emailBody = template.fileContent
                .Replace("{{customerName}}", $"{customerPref.Lastname} {(string.IsNullOrWhiteSpace(customerPref.Middlename) ? "" : customerPref.Middlename + " ")}{customerPref.Firstname}")
                .Replace("{{amount}}", amount.ToString("N2"))
                .Replace("{{accountNumber}}", MaskAccountNumber(accountNumber, '*'))
                .Replace("{{transactionDateTime}}", date)
                .Replace("{{narration}}", narration)
                .Replace("{{transactionRef}}", sessionId)
                .Replace("{{transactionType}}", transactionType.ToUpper())
                .Replace("{{transactionTypeDesc}}", transactionType)
                .Replace("{{newBalance}}", newBalance)
                .Replace("{{year}}", DateTime.Now.Date.ToString());

            var emailNotificationPayload = new Message
            {
                Type = "email",
                IsInline = true,
                To = customerPref.Email,
                Subject = $"{transactionType} Alert!",
                MessageBody = emailBody
            };

            await _rabbitMqPublisher.PublishAsync<Message>(_daprSettings.NotificationTopic, emailNotificationPayload);
        }

        private async Task SendSmsAsync(dynamic customerPref, string transactionType, string accountNumber, decimal amount, string date, string narration, string balance)
        {
            if (customerPref?.NotifyBySms != true) return;

            var notificationPayload = new Message
            {
                Type = "sms",
                TemplateName = "fundtransferSMSTemplate",
                To = ConvertToInternationalFormat(customerPref.PhoneNumber),
                Params = new
                {
                    transactionType,
                    accountNumber = MaskAccountNumber(accountNumber, 'X'),
                    amount = amount.ToString("N2"),
                    date,
                    narration,
                    balance = decimal.TryParse(balance, out var balanceDecimal)
                        ? balanceDecimal.ToString("N2")
                        : "0.00"
                }
            };

            _logger.LogInformation($"Publishing SMS message to notification to Queue...");
            await _rabbitMqPublisher.PublishAsync<Message>(_daprSettings.NotificationTopic, notificationPayload);
            _logger.LogInformation($"Published payload {JsonConvert.SerializeObject(notificationPayload)} to queue.");
            await RecordSuccessfullFundtransferNotification(customerPref.Id, customerPref.PhoneNumber, NotificationType.FundTransfer, NotificationChannel.Sms);
        }

        private string ConvertToStandardFormat(string input, TimeOnly timesection)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                var now = DateTime.Now;
                var merged = new DateTime(now.Year, now.Month, now.Day,
                                          timesection.Hour, timesection.Minute, timesection.Second);
                return merged.ToString("yyyy-MM-dd HH:mm:ss");
            }

            string[] formats =
            {
                "dd MMM yyyy HH:mm:ss",
                "dd MMM yyyy",
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-dd"
            };

            if (!DateTime.TryParseExact(
                    input,
                    formats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedDate))
            {
                throw new FormatException("Invalid date format.");
            }

            bool hasNoTime = parsedDate.TimeOfDay == TimeSpan.Zero &&
                             (input.Length == 10 || input.Length == 11 + 4);

            if (hasNoTime)
            {
                parsedDate = parsedDate.Date
                    .AddHours(timesection.Hour)
                    .AddMinutes(timesection.Minute)
                    .AddSeconds(timesection.Second);
            }

            return parsedDate.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private string ConvertToInternationalFormat(string phoneNumber)
        {
            if (Regex.IsMatch(phoneNumber, "^0\\d{10}$"))
            {
                return "+234" + phoneNumber.Substring(1);
            }

            if (Regex.IsMatch(phoneNumber, "^\\+234\\d{10}$"))
            {
                return phoneNumber;
            }

            throw new ArgumentException("Invalid Phone number format.");
        }

        private static string MaskAccountNumber(string accountNumber, char character)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
                return accountNumber;

            if (!Regex.IsMatch(accountNumber, @"^\d{10}$"))
                return accountNumber;

            if (accountNumber.Length != 10)
                return accountNumber;

            // show first 2 and last 2
            string first = accountNumber.Substring(0, 3);
            string last = accountNumber.Substring(7, 3);
            string maskedMiddle = new string(character, 4);

            return $"{first}{maskedMiddle}{last}";
        }

        private async Task RecordSuccessfullFundtransferNotification(string userId, string phoneNumber, NotificationType notificationType, NotificationChannel channel)
        {
            _logger.LogInformation($"Creating notification record for userId: {userId} on PhoneNumebr:{phoneNumber}");
            var notification = new NotificationRecord
            {
                UserId = userId,
                PhoneNumber = phoneNumber,
                NotificationChannel = channel,
                NotificationType = notificationType
            };
            await _context.NotificationRecords.AddAsync(notification);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Notification added successfuly  for userID {userId}");
        }
    }
}
