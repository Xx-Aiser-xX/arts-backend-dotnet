using Arts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arts.Infrastructure.Persistence.Configurations;

public class SocialNetworkConfiguration : IEntityTypeConfiguration<SocialNetwork>
{
    public void Configure(EntityTypeBuilder<SocialNetwork> builder)
    {
        builder.ToTable("social_networks");

        builder.HasKey(sn => sn.Id);
        builder.Property(sn => sn.Id).HasColumnName("id");

        builder.Property(sn => sn.Link)
            .HasColumnName("link")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(sn => sn.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(sn => sn.IsDeleted).HasColumnName("is_deleted").IsRequired();

        builder.HasOne(sn => sn.User)
            .WithMany(u => u.SocialNetworks)
            .HasForeignKey(sn => sn.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(sn => new { sn.IsDeleted, sn.UserId })
            .HasDatabaseName("idx_social_networks_deleted_user");

        builder.HasQueryFilter(sn => !sn.IsDeleted);
    }
}