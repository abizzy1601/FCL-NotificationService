using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using NotificationService.Worker.Services;

namespace NotificationService.Worker.ServiceBroker
{
    public class ServiceBrokerTransactionListener : BackgroundService
    {
        private readonly string _connectionString;
        private readonly ILogger<ServiceBrokerTransactionListener> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public ServiceBrokerTransactionListener(IServiceScopeFactory scopeFactory, ILogger<ServiceBrokerTransactionListener> logger,
        IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            _connectionString = config.GetConnectionString("T24Connection")
                ?? throw new ArgumentNullException(nameof(config), "T24Connection not found");
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            _logger.LogInformation("Service Broker Transaction Listener started");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await ListenForNotifications(token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Service Broker listener loop");
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                }
            }

            _logger.LogInformation("Service Broker Transaction Listener stopped");
        }

        private async Task ListenForNotifications(CancellationToken token)
        {
            using var receiveConnection = new SqlConnection(_connectionString);
            using var endConnection = new SqlConnection(_connectionString);

            try
            {
                await receiveConnection.OpenAsync(token);
                await endConnection.OpenAsync(token);

                const string sql = @"
               WAITFOR (
                    RECEIVE TOP(1)
                        conversation_handle,
                        message_type_name,
                        CAST(message_body AS NVARCHAR(MAX)) AS message_body
                    FROM TransactionNotificationQueue
                ), TIMEOUT 5000;
            ";

                using var cmd = new SqlCommand(sql, receiveConnection);
                cmd.CommandTimeout = 0;


                using var reader = await cmd.ExecuteReaderAsync(token);

                if (!reader.HasRows)
                    return;

                while (await reader.ReadAsync(token))
                {
                    var handle = reader["conversation_handle"] as Guid?;
                    var messageType = reader["message_type_name"]?.ToString();
                    var json = reader["message_body"]?.ToString(); 

                    if (handle == null || string.IsNullOrWhiteSpace(json))
                    {
                        _logger.LogWarning("Received empty or malformed message from Service Broker");
                        continue;
                    }

                    _logger.LogDebug("Received message: Type={MessageType}, Handle={Handle}",
                        messageType, handle);

                    if (messageType == "TransactionNotificationMessage")
                    {
                        await HandleTransactionNotification(json);
                    }
                    else if (messageType == "http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog")
                    {
                        _logger.LogInformation("Conversation ended.");
                    }
                    else if (messageType == "http://schemas.microsoft.com/SQL/ServiceBroker/Error")
                    {
                        _logger.LogError("Service Broker error: {Error}", json);
                    }

                    await EndConversation(endConnection, handle.Value, token);
                }

            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while listening for notifications");
                throw;
            }
        }

        private async Task EndConversation(SqlConnection connection, Guid handle, CancellationToken token)
        {
            try
            {
                const string sql = "END CONVERSATION @handle;";
                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@handle", handle);
                await cmd.ExecuteNonQueryAsync(token);

                _logger.LogDebug("Ended conversation {Handle}", handle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to end conversation {Handle}", handle);
            }
        }

        private async Task HandleTransactionNotification(string json)
        {
            try
            {
                //Parse transaction data
                var transaction = JsonConvert.DeserializeObject<DTOs.TransactionNotification>(json);

                _logger.LogInformation(
                    $"Processing transaction notification: ID={transaction.TRANSACTION_ID}, Type={transaction.TRANSACTION_TYPE}");

                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<ITransactionProcessor>();

                // Process the transaction
                await processor.ProcessAsync(transaction);

                _logger.LogInformation("Successfully processed transaction {TransactionId}",
                    transaction.TRANSACTION_ID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling transaction notification: {Json}", json);
                throw;
            }
        }
    }
}
