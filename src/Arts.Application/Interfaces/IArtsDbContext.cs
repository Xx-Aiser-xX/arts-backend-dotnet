using Arts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Arts.Application.Interfaces;

public interface IArtsDbContext
{
    DbSet<User> Users { get; }
    DbSet<Art> Arts { get; }
    DbSet<Interaction> Interactions { get; }
    DbSet<Tag> Tags { get; }
    DbSet<ArtTag> ArtTags { get; }
    DbSet<Comment> Comments { get; }
    DbSet<Sub> Subscriptions { get; }
    DbSet<UserPreferences> UserPreferences { get; }
    DbSet<SocialNetwork> SocialNetworks { get; }

    DatabaseFacade Database { get; }
    ChangeTracker ChangeTracker { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}