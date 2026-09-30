using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using NotificationService.Worker.Data.Entities;

namespace NotificationService.Worker.Data.EFConfigurations
{
    public class NotificationRecordConfiguration : IEntityTypeConfiguration<NotificationRecord>
    {
        public void Configure(EntityTypeBuilder<NotificationRecord> builder)
        {
            builder.ToTable("NotificationRecords");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.PhoneNumber)
                   .IsRequired();

            builder.HasOne(x => x.User)
                   .WithOne()
                   .HasForeignKey<NotificationRecord>(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
