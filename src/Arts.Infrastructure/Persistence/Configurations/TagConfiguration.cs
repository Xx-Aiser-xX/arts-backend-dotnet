using Arts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arts.Infrastructure.Persistence.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("tags");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");

        builder.Property(t => t.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();
        builder.HasIndex(t => t.Name).IsUnique();

        builder.Property(t => t.IsDeleted).HasColumnName("is_deleted").IsRequired();

        builder.HasIndex(t => new { t.Name, t.IsDeleted })
            .HasDatabaseName("idx_tags_name_deleted");

        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}