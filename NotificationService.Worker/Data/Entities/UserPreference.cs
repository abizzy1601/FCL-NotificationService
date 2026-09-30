using System.ComponentModel.DataAnnotations;

namespace NotificationService.Worker.Data.Entities
{
    public class UserPreference
    {
        [Key]
        public string UserId { get; set; }
        public bool NotifyByMail { get; set; }
        public bool NotifyBySms { get; set; }
    }
}
