using Glinter.Modules.Communication.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Communication.Infrastructure.Persistence.Configurations;

public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("notification_preferences");

        builder.HasKey(x => x.UserId);

        builder.Property(x => x.InAppEnabled)
            .IsRequired();

        builder.Property(x => x.EmailEnabled)
            .IsRequired();

        builder.Property(x => x.PushEnabled)
            .IsRequired();

        builder.Property(x => x.ChatMessageNotificationsEnabled)
            .IsRequired();

        builder.Property(x => x.SystemNotificationsEnabled)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();
    }
}
