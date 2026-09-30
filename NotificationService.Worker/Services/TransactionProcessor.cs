using NotificationService.Worker.DTOs;

namespace NotificationService.Worker.Services
{
    public interface ITransactionProcessor
    {
        Task ProcessAsync(TransactionNotification transaction);
    }

    public class TransactionProcessor : ITransactionProcessor
    {
        private readonly ILogger<TransactionProcessor> _logger;
        private readonly INotificationService _notificationService;

        public TransactionProcessor(
            ILogger<TransactionProcessor> logger, INotificationService notificationService)
        {
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task ProcessAsync(TransactionNotification transaction)
        {
            await _notificationService.SendTransferSuccessNotificationAsync(transaction);
        }
    }
}
