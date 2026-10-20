using Arts.Application.DTOs.Requests.Arts;
using Arts.Application.DTOs.Responses;
using Arts.Application.DTOs.Responses.Arts;
using Arts.Application.Interfaces;
using Arts.Application.Services.Arts;
using Arts.Infrastructure.Configuration;

namespace Arts.Infrastructure.Caching.Decorators;

public class CachedArtService(
    IArtService innerService,
    ICacheService cacheService) : IArtService
{
    public async Task<ArtResponse> GetArtAndAuthorByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await cacheService.GetOrCreateAsync(
            key: $"arts:{id}",
            factory: () => innerService.GetArtAndAuthorByIdAsync(id, ct),
            expiration: TimeSpan.FromHours(2),
            ct: ct);
    }

    public async Task<PagedResponse<ArtCardResponse>> GetArtByAuthorIdAsync(Guid authorId, int page, int size, CancellationToken ct = default)
    {
        var cacheKey = $"authors:{authorId}:arts:p{page}:s{size}";

        return await cacheService.GetOrCreateAsync(
            key: cacheKey,
            policyName: CachePolicy.AuthorsArts,
            factory: () => innerService.GetArtByAuthorIdAsync(authorId, page, size, ct),
            ct: ct);
    }

    public async Task<PagedResponse<ArtCardResponse>> SearchArtsByTagNameAndNameAsync(string query, int page, int size,
        CancellationToken ct = default)
    {
        var cacheKey = $"search:arts:q{query.ToLower().Trim()}:p{page}:s{size}";

        return await cacheService.GetOrCreateAsync(
            key: cacheKey,
            policyName: CachePolicy.Search,
            factory: () => innerService.SearchArtsByTagNameAndNameAsync(query, page, size, ct),
            ct: ct);
    }

    public async Task<ArtResponse> CreateAsync(ArtCreateRequest request, CancellationToken ct = default)
    {
        var response = await innerService.CreateAsync(request, ct);

        await cacheService.RemoveByPrefixAsync($"authors:{response.Author.Id}:arts", ct);
        await cacheService.RemoveByPrefixAsync("search:arts", ct);
        await cacheService.RemoveAsync($"users:{response.Author.Id}", ct);
        return response;
    }

    public async Task<ArtResponse> UpdateAsync(ArtUpdateRequest request, CancellationToken ct = default)
    {
        var response = await innerService.UpdateAsync(request, ct);

        await cacheService.RemoveAsync($"arts:{response.Id}", ct);
        await cacheService.RemoveByPrefixAsync($"authors:{response.Author.Id}:arts", ct);
        await cacheService.RemoveByPrefixAsync("search:arts", ct);

        return response;
    }

    public async Task<Guid> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var authorId = await innerService.DeleteAsync(id, ct);

        await cacheService.RemoveAsync($"arts:{id}", ct);
        await cacheService.RemoveByPrefixAsync($"authors:{authorId}:arts", ct);
        await cacheService.RemoveByPrefixAsync("search:arts", ct);

        return authorId;
    }

    public async Task<bool> LikeArtAsync(Guid id, CancellationToken ct = default)
    {
        var liked = await innerService.LikeArtAsync(id, ct);
        await cacheService.RemoveAsync($"arts:{id}", ct);
        return liked;
    }

    public Task ViewArtAsync(Guid artId, CancellationToken ct = default)
        => innerService.ViewArtAsync(artId, ct);

    public Task<bool> IsLikeArtAsync(Guid? id, CancellationToken ct = default)
    => innerService.IsLikeArtAsync(id, ct);

    public Task<PagedResponse<ArtCardResponse>> GetLikedArtsByCurrentUserAsync(int page, int size, CancellationToken ct = default)
        => innerService.GetLikedArtsByCurrentUserAsync(page, size, ct);

    public Task<PagedResponse<ArtCardResponse>> GetFeedAsync(string type, int page, int size, CancellationToken ct = default)
        => innerService.GetFeedAsync(type, page, size, ct);
}