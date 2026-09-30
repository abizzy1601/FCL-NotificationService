namespace NotificationService.Worker.Data.Entities
{
    public class User
    {
        public string Id { get; set; }
        public string Firstname { get; set; }
        public string? Middlename { get; set; }
        public string Lastname { get; set; }
        public string? Email { get; set; }
        public string PhoneNumber { get; set; }
        public string? CustomerId { get; set; }
    }
}
