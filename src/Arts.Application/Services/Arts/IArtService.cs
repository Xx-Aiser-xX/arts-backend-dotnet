using Arts.Application.DTOs.Requests.Arts;
using Arts.Application.DTOs.Responses;
using Arts.Application.DTOs.Responses.Arts;

namespace Arts.Application.Services.Arts;

public interface IArtService
{
    Task<ArtResponse> GetArtAndAuthorByIdAsync(Guid id, CancellationToken ct = default);
    
    Task<ArtResponse> CreateAsync(ArtCreateRequest request, CancellationToken ct = default);
    Task<ArtResponse> UpdateAsync(ArtUpdateRequest request, CancellationToken ct = default);
    Task<Guid> DeleteAsync(Guid id, CancellationToken ct = default);
    
    Task<bool> LikeArtAsync(Guid id, CancellationToken ct = default);
    Task<bool> IsLikeArtAsync(Guid? id, CancellationToken ct = default);
    Task ViewArtAsync(Guid artId, CancellationToken ct = default);

    Task<PagedResponse<ArtCardResponse>> GetLikedArtsByCurrentUserAsync(int page, int size, CancellationToken ct = default);
    Task<PagedResponse<ArtCardResponse>> GetFeedAsync(string type, int page, int size, CancellationToken ct = default);
    Task<PagedResponse<ArtCardResponse>> SearchArtsByTagNameAndNameAsync(string query, int page, int size, CancellationToken ct = default);
    Task<PagedResponse<ArtCardResponse>> GetArtByAuthorIdAsync(Guid authorId, int page, int size, CancellationToken ct = default);
}