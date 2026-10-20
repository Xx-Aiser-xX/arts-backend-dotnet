using Arts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arts.Infrastructure.Persistence.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("comments");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");

        builder.Property(c => c.Text)
            .HasColumnName("text")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.PublicationTime).HasColumnName("publication_time").IsRequired();
        
        builder.Property(c => c.ArtId).HasColumnName("art_id").IsRequired();
        builder.Property(c => c.AuthorId).HasColumnName("author_id").IsRequired();
        builder.Property(c => c.IsDeleted).HasColumnName("is_deleted").IsRequired();

        builder.HasOne(c => c.Art)
            .WithMany(a => a.Comments)
            .HasForeignKey(c => c.ArtId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.ArtId, c.IsDeleted })
            .HasDatabaseName("idx_comments_art_deleted");

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}