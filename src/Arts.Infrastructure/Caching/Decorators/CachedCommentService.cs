using Arts.Application.DTOs.Requests.Comments;
using Arts.Application.DTOs.Responses;
using Arts.Application.DTOs.Responses.Comments;
using Arts.Application.Interfaces;
using Arts.Application.Services.Comments;
using Arts.Infrastructure.Configuration;

namespace Arts.Infrastructure.Caching.Decorators;

public class CachedCommentService(
    ICommentService innerService,
    ICacheService cacheService) : ICommentService
{
    public async Task<PagedResponse<CommentResponse>> GetCommentsByArtIdAsync(
        Guid artId, int page, int size, CancellationToken ct = default)
    {
        var cacheKey = $"arts:{artId}:comments:p{page}:s{size}";

        return await cacheService.GetOrCreateAsync(
            key: cacheKey,
            policyName: CachePolicy.Comments,
            factory: () => innerService.GetCommentsByArtIdAsync(artId, page, size, ct),
            ct: ct);
    }

    public async Task<CommentResponse> CreateAsync(CommentCreateRequest request, CancellationToken ct = default)
    {
        var response = await innerService.CreateAsync(request, ct);
        await cacheService.RemoveByPrefixAsync($"arts:{request.ArtId}:comments", ct);
        return response;
    }

    public async Task<CommentResponse> UpdateAsync(CommentUpdateRequest request, CancellationToken ct = default)
    {
        var response = await innerService.UpdateAsync(request, ct);
        await cacheService.RemoveByPrefixAsync($"arts:{response.ArtId}:comments", ct);
        return response;
    }

    public async Task<Guid> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var artId = await innerService.DeleteAsync(id, ct);
        await cacheService.RemoveByPrefixAsync($"arts:{artId}:comments", ct);
        return artId;
    }
}