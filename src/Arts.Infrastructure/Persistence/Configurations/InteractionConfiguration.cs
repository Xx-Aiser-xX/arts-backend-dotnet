using Arts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arts.Infrastructure.Persistence.Configurations;

public class InteractionConfiguration : IEntityTypeConfiguration<Interaction>
{
    public void Configure(EntityTypeBuilder<Interaction> builder)
    {
        builder.ToTable("interactions");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id");

        builder.Property(i => i.Like).HasColumnName("is_like").IsRequired();
        builder.Property(i => i.View).HasColumnName("is_view").IsRequired();
        builder.Property(i => i.LikedAt).HasColumnName("liked_at");
        builder.Property(i => i.ViewAt).HasColumnName("view_at");
        builder.Property(i => i.IsDeleted).HasColumnName("is_deleted").IsRequired();

        builder.Property(i => i.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(i => i.ArtId).HasColumnName("art_id").IsRequired();

        builder.HasIndex(i => new { i.ArtId, i.UserId }).IsUnique();

        builder.HasOne(i => i.User)
            .WithMany(u => u.Interactions)
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Art)
            .WithMany(a => a.Interactions)
            .HasForeignKey(i => i.ArtId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.IsDeleted, i.UserId, i.ArtId })
            .HasDatabaseName("idx_interactions_deleted_user_art");
        builder.HasIndex(i => new { i.IsDeleted, i.Like, i.LikedAt })
            .HasDatabaseName("idx_interactions_deleted_like_likedAt");

        builder.HasQueryFilter(i => !i.IsDeleted);
    }
}