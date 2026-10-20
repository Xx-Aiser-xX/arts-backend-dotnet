using Arts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arts.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");

        builder.Property(u => u.UserName)
            .HasColumnName("user_name")
            .HasMaxLength(60)
            .IsRequired();
            
        builder.HasIndex(u => u.UserName).IsUnique();

        builder.Property(u => u.Description)
            .HasColumnName("description")
            .HasColumnType("TEXT")
            .HasMaxLength(5000);

        builder.Property(u => u.PhotoUrl)
            .HasColumnName("photo_url")
            .HasMaxLength(2000);

        builder.Property(u => u.CountJobs)
            .HasColumnName("count_jobs")
            .IsRequired();
        
        builder.Property(u => u.CountSubscriptions)
            .HasColumnName("count_subscriptions")
            .IsRequired();
        
        builder.Property(u => u.CountSubscribers)
            .HasColumnName("count_subscribers")
            .IsRequired();
        
        
        builder.Property(u => u.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();

        builder.HasIndex(u => new { u.IsDeleted, u.UserName })
            .HasDatabaseName("idx_users_deleted_username");

        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}