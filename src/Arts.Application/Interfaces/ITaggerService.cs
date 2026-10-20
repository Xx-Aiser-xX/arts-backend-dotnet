using Microsoft.AspNetCore.Http;

namespace Arts.Application.Interfaces;

public interface ITaggerService
{
    Task<List<string>> GetTagsForImageAsync(IFormFile file, CancellationToken ct = default);
}