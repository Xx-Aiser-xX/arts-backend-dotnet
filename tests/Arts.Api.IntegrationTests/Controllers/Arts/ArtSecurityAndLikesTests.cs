using System.Net;
using Arts.Api.IntegrationTests.Infrastructure;
using Arts.Domain.Entities;
using Arts.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Arts.Api.IntegrationTests;

public class ArtSecurityAndLikesTests(CustomWebApplicationFactory factory) 
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task LikeArt_WhenNotViewed_ShouldReturnBadRequest()
    {
        var author = new User(Guid.NewGuid(), $"author_{Guid.NewGuid():N}"[..20]);
        var userViewerId = Guid.NewGuid();
        var viewer = new User(userViewerId, $"viewer_{Guid.NewGuid():N}"[..20]);
        var art = new Art("Cool Unique Art", "Desc", $"http://minio/art_{Guid.NewGuid():N}.jpg", false, author);

        await SeedAsync(db =>
        {
            db.Users.AddRange(author, viewer);
            db.Arts.Add(art);
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/arts/like?id={art.Id}");
        request.Headers.Add("X-User-Id", userViewerId.ToString());

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateArt_WhenUserIsNotOwner_ShouldReturnUnauthorized()
    {
        var realAuthor = new User(Guid.NewGuid(), $"real_{Guid.NewGuid():N}"[..20]);
        var hackerId = Guid.NewGuid();
        var hacker = new User(hackerId, $"hacker_{Guid.NewGuid():N}"[..20]);
        var art = new Art("Original Name", "Desc", $"http://minio/art_{Guid.NewGuid():N}.jpg", false, realAuthor);

        await SeedAsync(db =>
        {
            db.Users.AddRange(realAuthor, hacker);
            db.Arts.Add(art);
        });

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(art.Id.ToString()), "Id");
        form.Add(new StringContent("Hacked Name"), "Name");
        form.Add(new StringContent("Hacked Desc"), "Description");
        form.Add(new StringContent("false"), "IsNsfw");

        using var request = new HttpRequestMessage(HttpMethod.Put, "/arts") { Content = form };
        request.Headers.Add("X-User-Id", hackerId.ToString());

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var artInDb = await GetFromDbAsync(db => db.Arts.FirstAsync(a => a.Id == art.Id));
        artInDb.Name.Should().Be("Original Name");
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