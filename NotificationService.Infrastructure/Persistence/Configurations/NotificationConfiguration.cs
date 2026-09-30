using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Core.Entities;

namespace NotificationService.Infrastructure.Persistence.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.UserId).HasMaxLength(64);
            builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(x => x.Channel).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Body).IsRequired().HasMaxLength(4000);
            builder.Property(x => x.ActionUrl).HasMaxLength(500);
            builder.Property(x => x.EventId).IsRequired().HasMaxLength(150);
            builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
            builder.Property(x => x.ReadAt).HasColumnType("datetime2");
            builder.Property(x => x.ExpiresAt).HasColumnType("datetime2");

            var metadataComparer = new ValueComparer<Dictionary<string, object?>?>(
                (a, b) => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(b, (JsonSerializerOptions?)null),
                v => v == null ? 0 : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null).GetHashCode(),
                v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, object?>>(JsonSerializer.Serialize(v, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null));

            builder.Property(x => x.Metadata)
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => string.IsNullOrEmpty(v) ? null : JsonSerializer.Deserialize<Dictionary<string, object?>>(v, (JsonSerializerOptions?)null))
                .HasColumnType("nvarchar(max)")
                .Metadata.SetValueComparer(metadataComparer);

            // Inbox pagination: WHERE UserId = @id ORDER BY CreatedAt DESC
            builder.HasIndex(x => new { x.UserId, x.CreatedAt })
                   .HasDatabaseName("IX_Notifications_UserId_CreatedAt");

            // Unread badge / unread list (personal notifications only)
            builder.HasIndex(x => new { x.UserId, x.IsRead })
                   .HasDatabaseName("IX_Notifications_UserId_IsRead");
            builder.HasIndex(x => new { x.UserId, x.ExpiresAt })
                   .HasDatabaseName("IX_Notifications_UserId_ExpiresAt");

            // Idempotency: a redelivered message must never create a duplicate row
            builder.HasIndex(x => x.EventId)
                   .IsUnique()
                   .HasDatabaseName("UX_Notifications_EventId");
        }
    }
}
