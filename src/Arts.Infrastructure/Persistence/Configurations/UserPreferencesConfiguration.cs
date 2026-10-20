using Arts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arts.Infrastructure.Persistence.Configurations;

public class UserPreferencesConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        builder.ToTable("user_preferences");

        builder.HasKey(up => up.Id);
        builder.Property(up => up.Id).HasColumnName("id");

        builder.Property(up => up.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(up => up.IsDeleted).HasColumnName("is_deleted").IsRequired();

        builder.HasOne(up => up.User)
            .WithMany()
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(up => !up.IsDeleted);
    }
}