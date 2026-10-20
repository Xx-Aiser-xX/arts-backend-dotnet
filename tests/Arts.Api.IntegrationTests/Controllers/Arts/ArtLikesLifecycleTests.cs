using System.Net;
using System.Net.Http.Json;
using Arts.Api.IntegrationTests.Infrastructure;
using Arts.Domain.Entities;
using Arts.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Arts.Api.IntegrationTests;

public class ArtLikesLifecycleTests(CustomWebApplicationFactory factory) 
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Art_ViewAndLikeLifecycle_ShouldCorrectlyUpdateCountersAndStatus()
    {
        var author = new User(Guid.NewGuid(), $"artist_{Guid.NewGuid():N}"[..20]);
        var viewerId = Guid.NewGuid();
        var viewer = new User(viewerId, $"viewer_{Guid.NewGuid():N}"[..20]);
        var art = new Art("Interactive Art", "Desc", $"http://minio/{Guid.NewGuid():N}.jpg", false, author);

        await SeedAsync(db =>
        {
            db.Users.AddRange(author, viewer);
            db.Arts.Add(art);
        });

        using var viewRequest = new HttpRequestMessage(HttpMethod.Get, $"/arts/with-author?id={art.Id}");
        viewRequest.Headers.Add("X-User-Id", viewerId.ToString());
        
        var viewResponse = await _client.SendAsync(viewRequest);
        viewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var artAfterView = await GetFromDbAsync(db => db.Arts.FirstAsync(a => a.Id == art.Id));
        artAfterView.CountViews.Should().Be(1);

        using var isLikeBeforeRequest = new HttpRequestMessage(HttpMethod.Get, $"/arts/like?id={art.Id}");
        isLikeBeforeRequest.Headers.Add("X-User-Id", viewerId.ToString());

        var isLikeBeforeResponse = await _client.SendAsync(isLikeBeforeRequest);
        isLikeBeforeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var isLikedBefore = await isLikeBeforeResponse.Content.ReadFromJsonAsync<bool>();
        isLikedBefore.Should().BeFalse();

        using var likeRequest = new HttpRequestMessage(HttpMethod.Post, $"/arts/like?id={art.Id}");
        likeRequest.Headers.Add("X-User-Id", viewerId.ToString());

        var likeResponse = await _client.SendAsync(likeRequest);
        likeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var artAfterLike = await GetFromDbAsync(db => db.Arts.FirstAsync(a => a.Id == art.Id));
        artAfterLike.CountLikes.Should().Be(1);

        using var isLikeAfterRequest = new HttpRequestMessage(HttpMethod.Get, $"/arts/like?id={art.Id}");
        isLikeAfterRequest.Headers.Add("X-User-Id", viewerId.ToString());
        var isLikedAfter = await (await _client.SendAsync(isLikeAfterRequest)).Content.ReadFromJsonAsync<bool>();
        isLikedAfter.Should().BeTrue();

        using var unlikeRequest = new HttpRequestMessage(HttpMethod.Post, $"/arts/like?id={art.Id}");
        unlikeRequest.Headers.Add("X-User-Id", viewerId.ToString());

        var unlikeResponse = await _client.SendAsync(unlikeRequest);
        unlikeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var artAfterUnlike = await GetFromDbAsync(db => db.Arts.FirstAsync(a => a.Id == art.Id));
        artAfterUnlike.CountLikes.Should().Be(0);
    }

    private async Task SeedAsync(Action<ArtsDbContext> action)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ArtsDbContext>();
        action(db);
        await db.SaveChangesAsync();
    }

    private async Task<T> GetFromDbAsync<T>(Func<ArtsDbContext, Task<T>> query)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ArtsDbContext>();
        return await query(db);
    }
}