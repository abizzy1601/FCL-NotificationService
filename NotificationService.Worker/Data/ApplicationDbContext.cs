using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NotificationService.Worker.Data.Entities;

namespace NotificationService.Worker.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserPreference> CustomerPreferences { get; set; }
        public DbSet<NotificationRecord> NotificationRecords { get; set; }
    }
}
