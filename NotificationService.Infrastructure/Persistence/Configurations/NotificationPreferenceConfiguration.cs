using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Core.Entities;

namespace NotificationService.Infrastructure.Persistence.Configurations
{
    public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
    {
        public void Configure(EntityTypeBuilder<NotificationPreference> builder)
        {
            builder.ToTable("NotificationPreferences");
            builder.HasKey(x => x.UserId);
            builder.Property(x => x.UserId).HasMaxLength(64);
            builder.Property(x => x.MutedCategories).HasColumnType("nvarchar(max)");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        }
    }
}
