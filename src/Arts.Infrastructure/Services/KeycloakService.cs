using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Arts.Application.DTOs.Requests.Keycloak;
using Arts.Application.DTOs.Responses.Keycloak;
using Arts.Application.Interfaces;
using Arts.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arts.Infrastructure.Services;

public class KeycloakService(
    HttpClient httpClient, 
    IOptions<KeycloakOptions> options,
    ILogger<KeycloakService> logger,
    IMemoryCache memoryCache) : IKeycloakService
{
    private readonly KeycloakOptions _options = options.Value;

    public async Task<KeycloakUserResponse> RegisterUserAsync(KeycloakRegisterRequest request, CancellationToken ct = default)
    {
        var adminToken = await GetAdminTokenAsync(ct);
        var createUrl = $"{_options.BaseUrl}/admin/realms/{_options.Realm}/users";
        var payload = new
        {
            username = request.Username,
            email = request.Email,
            enabled = true,
            credentials = new[]
            {
                new
                {
                    type = "password",
                    value = request.Password,
                    temporary = false
                }
            }
        };

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, createUrl)
        {
            Content = JsonContent.Create(payload)
        };
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await httpClient.SendAsync(requestMessage, ct);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            logger.LogWarning("Keycloak отклонил регистрацию: пользователь {Username} или email {Email} уже существует", 
                request.Username, request.Email);
            throw new InvalidOperationException(
                $"Пользователь с ником '{request.Username}' или email '{request.Email}' уже существует в Keycloak");
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Ошибка Keycloak при создании пользователя {Username}: {StatusCode} - {ErrorBody}", 
                request.Username, response.StatusCode, errorBody);
            throw new InvalidOperationException($"Ошибка Keycloak при создании пользователя: {response.StatusCode} - {errorBody}");
        }

        var locationHeader = response.Headers.Location;
        if (locationHeader is null)
            throw new InvalidOperationException("Keycloak не вернул заголовок Location с ID пользователя");

        var userId = locationHeader.Segments.Last();
        
        logger.LogInformation("Пользователь '{Username}' успешно создан в Keycloak (ID: {KeycloakUserId})", 
            request.Username, userId);

        return new KeycloakUserResponse(userId, request.Username, request.Email);
    }

    private async Task<string> GetAdminTokenAsync(CancellationToken ct)
    {
        const string cacheKey = "KeycloakAdminToken";

        if (memoryCache.TryGetValue(cacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
            return cachedToken;

        var tokenUrl = $"{_options.BaseUrl}/realms/master/protocol/openid-connect/token";
        var formParams = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "admin-cli",
            ["username"] = _options.AdminUsername,
            ["password"] = _options.AdminPassword
        };

        using var content = new FormUrlEncodedContent(formParams);
        var response = await httpClient.PostAsync(tokenUrl, content, ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Не удалось получить токен администратора Keycloak по адресу {TokenUrl}. Код ответа: {StatusCode}", 
                tokenUrl, response.StatusCode);
            throw new UnauthorizedAccessException(
                "Не удалось получить токен администратора Keycloak. Проверьте логин/пароль в appsettings.json");
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken: ct);
        
        var token = tokenResponse?.AccessToken 
            ?? throw new InvalidOperationException("Keycloak вернул пустой access_token");

        memoryCache.Set(cacheKey, token, TimeSpan.FromSeconds(50));
        return token;
    }

    private record KeycloakTokenResponse([property: JsonPropertyName("access_token")] string AccessToken);
}