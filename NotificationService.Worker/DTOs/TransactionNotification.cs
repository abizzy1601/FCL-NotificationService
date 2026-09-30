
namespace NotificationService.Worker.DTOs
{

    public class TransactionNotification
    {
        public string TRANSACTION_ID { get; set; }
        public string TRANSACTION_TYPE { get; set; }
        public string DEBIT_ACCT_NO { get; set; }
        public decimal DEBIT_AMOUNT { get; set; }
        public string CREDIT_ACCT_NO { get; set; }
        public decimal CREDIT_AMOUNT { get; set; }
        public string PROCESSING_DATE { get; set; }
        public string PAYMENT_DETAILS { get; set; }
        public string DEBIT_CUSTOMER_ID { get; set; }
        public string CREDIT_CUSTOMER_ID { get; set; }
        public string CUSTOMER_FULL_NAME { get; set; }
        public string CUSTOMER_EMAIL { get; set; }
        public string CUSTOMER_SMS { get; set; }
        public DateTime SyncedAt { get; set; }
        public string SESSION_ID { get; set; }
        public string CREDIT_ACCT_RECID { get; set; }
        public string CREDIT_ACCOUNT_BALANCE { get; set; }
        public string DEBIT_ACCT_RECID { get; set; }
        public string DEBIT_ACCOUNT_BALANCE { get; set; }
        public string TRANSACTION_MESSAGE { get; set; }
    }
}
