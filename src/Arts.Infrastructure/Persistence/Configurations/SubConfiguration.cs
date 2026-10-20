using Arts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arts.Infrastructure.Persistence.Configurations;

public class SubConfiguration : IEntityTypeConfiguration<Sub>
{
    public void Configure(EntityTypeBuilder<Sub> builder)
    {
        builder.ToTable("subscriptions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");

        builder.Property(s => s.SubscriptionDate).HasColumnName("subscription_date").IsRequired();
        builder.Property(s => s.SubscriberId).HasColumnName("subscriber_id").IsRequired();
        builder.Property(s => s.TargetId).HasColumnName("target_id").IsRequired();
        builder.Property(s => s.IsDeleted).HasColumnName("is_deleted").IsRequired();

        builder.HasOne(s => s.Subscriber)
            .WithMany(u => u.Targets)
            .HasForeignKey(s => s.SubscriberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Target)
            .WithMany(u => u.Subscribers)
            .HasForeignKey(s => s.TargetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.SubscriberId, s.IsDeleted })
            .HasDatabaseName("idx_subscriptions_subscriber_deleted");
        builder.HasIndex(s => new { s.SubscriberId, s.TargetId })
            .IsUnique();

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}