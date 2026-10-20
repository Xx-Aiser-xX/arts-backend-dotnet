using System.Net;
using System.Net.Http.Json;
using Arts.Api.IntegrationTests.Infrastructure;
using Arts.Application.DTOs.Responses;
using Arts.Domain.Entities;
using Arts.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arts.Api.IntegrationTests;

public class UserControllerTests(CustomWebApplicationFactory factory) 
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Subscribe_WhenSubscribingToSelf_ShouldReturnBadRequest()
    {
        var userId = Guid.NewGuid();
        var user = new User(userId, "myself");
        await SeedAsync(db => db.Users.Add(user));

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/users/subscribe?id={userId}");
        request.Headers.Add("X-User-Id", userId.ToString());

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Subscribe_ToggleSubscription_ShouldIncrementAndDecrementCounters()
    {
        var subscriberId = Guid.NewGuid();
        var authorId = Guid.NewGuid();

        var subscriber = new User(subscriberId, "subscriber");
        var author = new User(authorId, "famous_artist");

        await SeedAsync(db => db.Users.AddRange(subscriber, author));

        using var subRequest = new HttpRequestMessage(HttpMethod.Post, $"/users/subscribe?id={authorId}");
        subRequest.Headers.Add("X-User-Id", subscriberId.ToString());
        
        var subResponse = await _client.SendAsync(subRequest);
        subResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var authorInDb = await GetFromDbAsync(db => db.Users.FirstAsync(u => u.Id == authorId));
        var subscriberInDb = await GetFromDbAsync(db => db.Users.FirstAsync(u => u.Id == subscriberId));

        authorInDb.CountSubscribers.Should().Be(1);
        subscriberInDb.CountSubscriptions.Should().Be(1);

        using var unsubRequest = new HttpRequestMessage(HttpMethod.Post, $"/users/subscribe?id={authorId}");
        unsubRequest.Headers.Add("X-User-Id", subscriberId.ToString());

        var unsubResponse = await _client.SendAsync(unsubRequest);
        unsubResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        authorInDb = await GetFromDbAsync(db => db.Users.FirstAsync(u => u.Id == authorId));
        subscriberInDb = await GetFromDbAsync(db => db.Users.FirstAsync(u => u.Id == subscriberId));

        authorInDb.CountSubscribers.Should().Be(0);
        subscriberInDb.CountSubscriptions.Should().Be(0);
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