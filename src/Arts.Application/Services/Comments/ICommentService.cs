using Arts.Application.DTOs.Requests;
using Arts.Application.DTOs.Requests.Comments;
using Arts.Application.DTOs.Responses;
using Arts.Application.DTOs.Responses.Comments;

namespace Arts.Application.Services.Comments;

public interface ICommentService
{
    Task<CommentResponse> CreateAsync(CommentCreateRequest request, CancellationToken ct = default);
    Task<CommentResponse> UpdateAsync(CommentUpdateRequest request, CancellationToken ct = default);
    Task<Guid> DeleteAsync(Guid id, CancellationToken ct = default);
    
    Task<PagedResponse<CommentResponse>> GetCommentsByArtIdAsync(Guid artId, int page, int size, CancellationToken ct = default);
}