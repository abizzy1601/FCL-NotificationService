using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Core.Entities;

namespace NotificationService.Infrastructure.Persistence.Configurations
{
    public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
    {
        public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
        {
            builder.ToTable("NotificationTemplates");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.Code).IsRequired().HasMaxLength(100);
            builder.Property(x => x.TitleTemplate).IsRequired().HasMaxLength(200);
            builder.Property(x => x.BodyTemplate).IsRequired().HasMaxLength(4000);
            builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(30);
            builder.Property(x => x.DefaultPriority).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.Locale).IsRequired().HasMaxLength(10);

            builder.HasIndex(x => new { x.Code, x.Locale })
                   .IsUnique()
                   .HasDatabaseName("UX_NotificationTemplates_Code_Locale");
        }
    }
}
