using Arts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arts.Infrastructure.Persistence.Configurations;

public class ArtTagConfiguration : IEntityTypeConfiguration<ArtTag>
{
    public void Configure(EntityTypeBuilder<ArtTag> builder)
    {
        builder.ToTable("art_tags");

        builder.HasKey(at => at.Id);
        builder.Property(at => at.Id).HasColumnName("id");

        builder.Property(at => at.ArtId).HasColumnName("art_id").IsRequired();
        builder.Property(at => at.TagId).HasColumnName("tag_id").IsRequired();
        builder.Property(at => at.IsDeleted).HasColumnName("is_deleted").IsRequired();

        builder.HasOne(at => at.Art)
            .WithMany(a => a.ArtTags)
            .HasForeignKey(at => at.ArtId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(at => at.Tag)
            .WithMany(t => t.ArtTags)
            .HasForeignKey(at => at.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(at => new { at.IsDeleted, at.ArtId })
            .HasDatabaseName("idx_art_tags_deleted_art");
        builder.HasIndex(at => new { at.IsDeleted, at.TagId })
            .HasDatabaseName("idx_art_tags_deleted_tag");

        builder.HasQueryFilter(at => !at.IsDeleted);
    }
}