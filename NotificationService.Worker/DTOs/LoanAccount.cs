namespace NotificationService.Worker.DTOs
{
    public class LoanAccount
    {
        public int Id { get; set; }
        public string? Arrangement { get; set; } = default!;
        public long Account { get; set; } = default!;
        public long Customer { get; set; } = default!;
        public string? CustomerName { get; set; } = default!;
        public string? PhoneNo { get; set; } = default!;
        public DateTime DisbursementDate { get; set; }
        public DateTime MaturityDate { get; set; }
        public decimal MonthlyInstalment { get; set; }
        public decimal PrincipalBalance { get; set; }
        public decimal LoanAmount { get; set; }
        public string? OdStatus { get; set; } = default!;
        public DateTime ReportDate { get; set; }
        public string Product { get; set; }
    }
}
