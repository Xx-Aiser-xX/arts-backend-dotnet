using System.Net;
using System.Net.Http.Json;
using Arts.Api.IntegrationTests.Infrastructure;
using Arts.Application.DTOs.Responses;
using Arts.Application.DTOs.Responses.Arts;
using Arts.Domain.Entities;
using Arts.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arts.Api.IntegrationTests;

public class ArtControllerTests(CustomWebApplicationFactory factory) 
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetFeed_WhenDatabaseIsEmpty_ShouldReturnEmptyPage()
    {
        var response = await _client.GetAsync("/arts/feed?type=latest&page=1&size=12");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<PagedResponse<ArtCardResponse>>();
        content.Should().NotBeNull();
        content!.Content.Should().BeEmpty();
        content.TotalElements.Should().Be(0);
    }

    [Fact]
    public async Task GetFeed_WhenArtsExist_ShouldReturnPopulatedFeed()
    {
        var author = new User(Guid.NewGuid(), $"author_{Guid.NewGuid():N}"[..20]);
        var art = new Art("Cyberpunk City", "Neon lights and rain", "http://minio/art1.jpg", false, author);
        
        await SeedDatabaseAsync(db =>
        {
            db.Users.Add(author);
            db.Arts.Add(art);
        });

        var response = await _client.GetAsync("/arts/feed?type=latest&page=1&size=12");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<ArtCardResponse>>();
        result.Should().NotBeNull();
        result!.Content.Should().Contain(a => a.Name == "Cyberpunk City");
    }

    [Fact]
    public async Task SearchArts_WhenQueryMatches_ShouldReturnFilteredResults()
    {
        var author = new User(Guid.NewGuid(), $"search_author_{Guid.NewGuid():N}"[..20]);
        var art1 = new Art("Futuristic Car", "Fast speed", "http://minio/car.jpg", false, author);
        var art2 = new Art("Ancient Dragon", "Fire and smoke", "http://minio/dragon.jpg", false, author);

        await SeedDatabaseAsync(db =>
        {
            db.Users.Add(author);
            db.Arts.AddRange(art1, art2);
        });

        var response = await _client.GetAsync("/arts/search?query=Dragon&page=1&size=12");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<ArtCardResponse>>();
        result.Should().NotBeNull();
        result!.Content.Should().ContainSingle(a => a.Name == "Ancient Dragon");
        result.Content.Should().NotContain(a => a.Name == "Futuristic Car");
    }

    [Fact]
    public async Task GetArtAndAuthorById_WhenArtExists_ShouldReturnArtWithAuthor()
    {
        var author = new User(Guid.NewGuid(), $"master_{Guid.NewGuid():N}"[..20]);
        var art = new Art("Masterpiece", "Best painting ever", "http://minio/pic.jpg", false, author);

        await SeedDatabaseAsync(db =>
        {
            db.Users.Add(author);
            db.Arts.Add(art);
        });

        var response = await _client.GetAsync($"/arts/with-author?id={art.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ArtResponse>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Masterpiece");
        result.Author.Should().NotBeNull();
        result.Author.UserName.Should().Be(author.UserName);
    }

    [Fact]
    public async Task CreateArt_WhenUnauthorized_ShouldReturn401Unauthorized()
    {
        var emptyContent = new MultipartFormDataContent();
        var response = await _client.PostAsync("/arts", emptyContent);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task SeedDatabaseAsync(Action<ArtsDbContext> action)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ArtsDbContext>();
        action(db);
        await db.SaveChangesAsync();
    }
}