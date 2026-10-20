using Arts.Application.Interfaces;
using Arts.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Arts.Infrastructure.Persistence;

public class ArtsDbContext : DbContext, IArtsDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Art> Arts => Set<Art>();
    public DbSet<Interaction> Interactions => Set<Interaction>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ArtTag> ArtTags => Set<ArtTag>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Sub> Subscriptions => Set<Sub>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<SocialNetwork> SocialNetworks => Set<SocialNetwork>();

    public ArtsDbContext(DbContextOptions<ArtsDbContext> options) 
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ArtsDbContext).Assembly);
    }
}