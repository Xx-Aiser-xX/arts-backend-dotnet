using System.Net.Http.Headers;
using System.Net.Http.Json;
using Arts.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Arts.Infrastructure.Services;

public class TaggerService(
    HttpClient httpClient, 
    ILogger<TaggerService> logger) : ITaggerService
{
    public async Task<List<string>> GetTagsForImageAsync(IFormFile file, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return [];

        using var content = new MultipartFormDataContent();

        await using var fileStream = file.OpenReadStream();
        using var streamContent = new StreamContent(fileStream);

        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

        content.Add(streamContent, "file", file.FileName);

        var response = await httpClient.PostAsync("/tags", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Сервис тегирования AI вернул ошибку {StatusCode}: {ErrorBody}", 
                response.StatusCode, errorBody);
            throw new InvalidOperationException($"Ошибка от сервиса тегирования ({response.StatusCode}): {errorBody}");
        }

        var tags = await response.Content.ReadFromJsonAsync<List<string>>(cancellationToken: ct);

        return tags ?? [];
    }
}