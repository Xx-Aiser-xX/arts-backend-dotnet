using Arts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arts.Infrastructure.Persistence.Configurations;

public class ArtConfiguration : IEntityTypeConfiguration<Art>
{
    public void Configure(EntityTypeBuilder<Art> builder)
    {
        builder.ToTable("arts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasColumnName("id");

        builder.Property(a => a.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(a => a.Name).IsUnique();

        builder.Property(a => a.Description)
            .HasColumnName("description")
            .HasColumnType("TEXT")
            .HasMaxLength(5000);

        builder.Property(a => a.ImageUrl)
            .HasColumnName("image_url")
            .HasMaxLength(2000)
            .IsRequired();

        builder.HasIndex(a => a.ImageUrl).IsUnique();

        builder.Property(a => a.CountLikes)
            .HasColumnName("count_likes")
            .IsRequired();
        
        builder.Property(a => a.CountViews)
            .HasColumnName("count_views")
            .IsRequired();
        
        builder.Property(a => a.IsNsfw)
            .HasColumnName("nsfw")
            .IsRequired();
        
        builder.Property(a => a.PublicationTime)
            .HasColumnName("publication_time")
            .IsRequired();
        
        builder.Property(a => a.AuthorId)
            .HasColumnName("user_id")
            .IsRequired();
        
        builder.Property(u => u.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();

        builder.HasOne(a => a.Author)
            .WithMany(u => u.Arts)
            .HasForeignKey(a => a.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.IsDeleted, a.AuthorId})
            .HasDatabaseName("idx_arts_deleted_user_id");
        
        builder.HasIndex(a => new { a.IsDeleted, a.PublicationTime})
            .HasDatabaseName("idx_arts_deleted_publication_time");
        
        
        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}