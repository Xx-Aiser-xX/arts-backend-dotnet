using Arts.Domain.Entities;

namespace Arts.Application.Services.Tags;

public interface ITagDomainService
{
    Task<List<Tag>> GetOrCreateTagsAsync(IEnumerable<string> tagNames, CancellationToken ct = default);
}