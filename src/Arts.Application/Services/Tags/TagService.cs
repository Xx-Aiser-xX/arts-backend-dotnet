using Arts.Application.DTOs.Responses.Tags;
using Arts.Application.Interfaces;
using Arts.Domain.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Arts.Application.Services.Tags;

public class TagService(IArtsDbContext dbContext) : ITagService, ITagDomainService
{
    public async Task<List<TagResponse>> GetTagsByArtIdAsync(Guid artId, CancellationToken ct = default)
    { 
        return await dbContext.ArtTags
            .Where(at => at.ArtId == artId)
            .Select(at => at.Tag)
            .ProjectToType<TagResponse>()
            .ToListAsync(ct);
    }
    
    public async Task<List<Tag>> GetOrCreateTagsAsync(IEnumerable<string> tagNames, CancellationToken ct = default)
    {
        var normalizedNames = tagNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .ToHashSet();

        if (normalizedNames.Count == 0)
            return [];

        var existingTags = await dbContext.Tags
            .Where(t => normalizedNames.Contains(t.Name))
            .ToListAsync(ct);

        var existingNames = existingTags.Select(t => t.Name).ToHashSet();

        var newTags = normalizedNames
            .Where(name => !existingNames.Contains(name))
            .Select(name => new Tag(name))
            .ToList();

        if (newTags.Count > 0)
            dbContext.Tags.AddRange(newTags);

        return [..existingTags, ..newTags];
    }
}
