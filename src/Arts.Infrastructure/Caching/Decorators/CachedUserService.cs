using Arts.Application.DTOs.Requests.Users;
using Arts.Application.DTOs.Responses;
using Arts.Application.DTOs.Responses.SocialNetworks;
using Arts.Application.DTOs.Responses.Users;
using Arts.Application.Interfaces;
using Arts.Application.Services.Users;

namespace Arts.Infrastructure.Caching.Decorators;

public class CachedUserService(
    IUserService innerService,
    ICacheService cacheService) : IUserService
{
    public async Task<UserResponse> GetUserAsync(Guid id, CancellationToken ct = default)
    {
        return await cacheService.GetOrCreateAsync(
            key: $"users:{id}",
            factory: () => innerService.GetUserAsync(id, ct),
            expiration: TimeSpan.FromHours(2),
            ct: ct);
    }

    public async Task<List<SocialNetworkResponse>> GetSocialNetworkUserAsync(Guid id, CancellationToken ct = default)
    {
        return await cacheService.GetOrCreateAsync(
            key: $"users:{id}:socials",
            factory: () => innerService.GetSocialNetworkUserAsync(id, ct),
            expiration: TimeSpan.FromHours(1),
            ct: ct);
    }

    public async Task<UserResponse> UpdateUserAsync(UserUpdateRequest request, CancellationToken ct = default)
    {
        var response = await innerService.UpdateUserAsync(request, ct);

        await cacheService.RemoveAsync($"users:{response.Id}", ct);
        await cacheService.RemoveAsync($"users:{response.Id}:socials", ct);

        return response;
    }

    public async Task<SubscribeResponse> SubscribeAsync(Guid idAuthor, CancellationToken ct = default)
    {
        var response = await innerService.SubscribeAsync(idAuthor, ct);

        await cacheService.RemoveAsync($"users:{response.SubscriberId}", ct);
        await cacheService.RemoveAsync($"users:{response.TargetId}", ct);

        return response;
    }
    
    public async Task<Guid> DeleteAsync(CancellationToken ct = default)
    {
        var userId = await innerService.DeleteAsync(ct);

        await cacheService.RemoveAsync($"users:{userId}", ct);
        await cacheService.RemoveAsync($"users:{userId}:socials", ct);

        return userId;
    }

    public Task<UserResponse> RegisterUserAsync(RegisterUserRequest request, CancellationToken ct = default)
        => innerService.RegisterUserAsync(request, ct);

    public Task<UserResponse> GetCurrentUserDtoAsync(CancellationToken ct = default)
        => innerService.GetCurrentUserDtoAsync(ct);

    public Task<UserMinResponse> GetCurrentUserMinDtoAsync(CancellationToken ct = default)
        => innerService.GetCurrentUserMinDtoAsync(ct);

    public Task<bool> IsSubscribeAsync(Guid authorId, CancellationToken ct = default)
        => innerService.IsSubscribeAsync(authorId, ct);

    public Task<PagedResponse<UserMinResponse>> GetSubscriptionsWithArtsAsync(int page, int size, CancellationToken ct = default)
        => innerService.GetSubscriptionsWithArtsAsync(page, size, ct);

    public Task<List<UserMinResponse>> GetRecentSubscriptionsAsync(int limit, CancellationToken ct = default)
        => innerService.GetRecentSubscriptionsAsync(limit, ct);
}