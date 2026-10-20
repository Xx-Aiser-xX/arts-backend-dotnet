using Arts.Application.DTOs.Responses.Tags;
using Arts.Application.Interfaces;
using Arts.Application.Services.Tags;
using Arts.Domain.Entities;

namespace Arts.Infrastructure.Caching.Decorators;

public class CachedTagService(
    ITagService innerService,
    ICacheService cacheService) : ITagService
{
    public async Task<List<TagResponse>> GetTagsByArtIdAsync(Guid artId, CancellationToken ct = default)
    {
        var cacheKey = $"arts:{artId}:tags";

        return await cacheService.GetOrCreateAsync(
            key: cacheKey,
            factory: () => innerService.GetTagsByArtIdAsync(artId, ct),
            expiration: TimeSpan.FromHours(2),
            ct: ct);
    }
}