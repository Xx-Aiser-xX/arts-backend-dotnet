using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Arts.Api.IntegrationTests.Infrastructure;
using Arts.Application.DTOs.Requests.Comments;
using Arts.Application.DTOs.Responses;
using Arts.Application.DTOs.Responses.Comments;
using Arts.Domain.Entities;
using Arts.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Arts.Api.IntegrationTests;

public class CommentControllerTests(CustomWebApplicationFactory factory) 
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateComment_WhenValidRequest_ShouldCreateAndReturnComment()
    {
        // Arrange
        var author = new User(Guid.NewGuid(), $"art_author_{Guid.NewGuid():N}"[..20]);
        var art = new Art("Artwork for Comment", "Desc", $"http://minio/{Guid.NewGuid():N}.jpg", false, author);
        var commenterId = Guid.NewGuid();
        var commenter = new User(commenterId, $"commenter_{Guid.NewGuid():N}"[..20]);

        await SeedAsync(db =>
        {
            db.Users.AddRange(author, commenter);
            db.Arts.Add(art);
        });

        var requestDto = new CommentCreateRequest(art.Id, "Отличная работа!");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/comments/art")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestDto), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-User-Id", commenterId.ToString());

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CommentResponse>();
        result.Should().NotBeNull();
        result!.Text.Should().Be("Отличная работа!");
        result.AuthorUserName.Should().Be(commenter.UserName);

        var commentInDb = await GetFromDbAsync(db => db.Comments.FirstOrDefaultAsync(c => c.Id == result.Id));
        commentInDb.Should().NotBeNull();
        commentInDb!.ArtId.Should().Be(art.Id);
    }

    [Fact]
    public async Task CreateComment_WhenArtNotFound_ShouldReturn404()
    {
        var commenterId = Guid.NewGuid();
        var commenter = new User(commenterId, $"commenter_{Guid.NewGuid():N}"[..20]);
        await SeedAsync(db => db.Users.Add(commenter));

        var nonExistentArtId = Guid.NewGuid();
        var requestDto = new CommentCreateRequest(nonExistentArtId, "Текст");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/comments/art")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestDto), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-User-Id", commenterId.ToString());

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetComments_ShouldReturnCommentsOrderedByDateDescending()
    {
        var author = new User(Guid.NewGuid(), $"author_{Guid.NewGuid():N}"[..20]);
        var art = new Art("Art with comments", "Desc", $"http://minio/{Guid.NewGuid():N}.jpg", false, author);
        var commenter = new User(Guid.NewGuid(), $"commenter_{Guid.NewGuid():N}"[..20]);

        var oldComment = new Comment("Старый комментарий", art, commenter);
        var newComment = new Comment("Новый комментарий", art, commenter);

        await SeedAsync(db =>
        {
            db.Users.AddRange(author, commenter);
            db.Arts.Add(art);
            db.Comments.AddRange(oldComment, newComment);
        });

        var response = await _client.GetAsync($"/comments/art?id={art.Id}&page=1&size=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<CommentResponse>>();
        result.Should().NotBeNull();
        result!.Content.Should().HaveCount(2);

        result.Content.First().Text.Should().Be("Новый комментарий");
        result.Content.Last().Text.Should().Be("Старый комментарий");
    }

    [Fact]
    public async Task DeleteComment_WhenUserIsNotOwner_ShouldReturnUnauthorized()
    {
        var author = new User(Guid.NewGuid(), $"author_{Guid.NewGuid():N}"[..20]);
        var art = new Art("Art for Delete", "Desc", $"http://minio/{Guid.NewGuid():N}.jpg", false, author);
        var commenter = new User(Guid.NewGuid(), $"real_commenter_{Guid.NewGuid():N}"[..20]);
        var comment = new Comment("Оригинальный коммент", art, commenter);

        var hackerId = Guid.NewGuid();
        var hacker = new User(hackerId, $"hacker_{Guid.NewGuid():N}"[..20]);

        await SeedAsync(db =>
        {
            db.Users.AddRange(author, commenter, hacker);
            db.Arts.Add(art);
            db.Comments.Add(comment);
        });

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/comments?id={comment.Id}");
        request.Headers.Add("X-User-Id", hackerId.ToString());

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var commentInDb = await GetFromDbAsync(db => db.Comments.FirstAsync(c => c.Id == comment.Id));
        commentInDb.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteComment_WhenOwnerDeletes_ShouldSoftDeleteAndDisappearFromQueries()
    {
        var author = new User(Guid.NewGuid(), $"author_{Guid.NewGuid():N}"[..20]);
        var art = new Art("Art for SoftDelete", "Desc", $"http://minio/{Guid.NewGuid():N}.jpg", false, author);
        var commenterId = Guid.NewGuid();
        var commenter = new User(commenterId, $"commenter_{Guid.NewGuid():N}"[..20]);
        var comment = new Comment("Удаляемый комментарий", art, commenter);

        await SeedAsync(db =>
        {
            db.Users.AddRange(author, commenter);
            db.Arts.Add(art);
            db.Comments.Add(comment);
        });

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/comments?id={comment.Id}");
        request.Headers.Add("X-User-Id", commenterId.ToString());

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var commentInDb = await GetFromDbAsync(db => 
            db.Comments.IgnoreQueryFilters().FirstAsync(c => c.Id == comment.Id));
        commentInDb.IsDeleted.Should().BeTrue();

        var getResponse = await _client.GetAsync($"/comments/art?id={art.Id}&page=1&size=10");
        var content = await getResponse.Content.ReadFromJsonAsync<PagedResponse<CommentResponse>>();
        
        content!.Content.Should().BeEmpty();
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