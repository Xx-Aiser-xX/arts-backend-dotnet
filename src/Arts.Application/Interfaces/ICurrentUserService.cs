using Arts.Domain.Entities;

namespace Arts.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid GetRequiredUserId();
    
    bool IsAuthenticated { get; }

    Task<User?> GetCurrentUserAsync(CancellationToken ct = default);
    Task<User> GetRequiredUserAsync(CancellationToken ct = default);
}