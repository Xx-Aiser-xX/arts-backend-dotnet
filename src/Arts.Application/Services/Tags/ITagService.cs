using Arts.Application.DTOs.Responses.Tags;

namespace Arts.Application.Services.Tags;

public interface ITagService
{
    Task<List<TagResponse>> GetTagsByArtIdAsync(Guid artId, CancellationToken ct = default);
}