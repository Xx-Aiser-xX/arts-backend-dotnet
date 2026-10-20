using System.Net;
using Arts.Api.IntegrationTests.Infrastructure;
using Arts.Domain.Entities;
using Arts.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arts.Api.IntegrationTests;

public class ConcurrencyCountersTests(CustomWebApplicationFactory factory) 
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private const int ConcurrentUsersCount = 40;

    [Fact]
    public async Task LikeArt_WhenLikedConcurrentlyBy40Users_ShouldNotLoseLikes()
    {
        // Arrange: создаем автора, арт и 40 зрителей, которые уже просмотрели арт
        var author = new User(Guid.NewGuid(), $"author_{Guid.NewGuid():N}"[..20]);
        var art = new Art("Viral Masterpiece", "Desc", $"http://minio/{Guid.NewGuid():N}.jpg", false, author);
        
        var viewers = Enumerable.Range(0, ConcurrentUsersCount)
            .Select(_ => new User(Guid.NewGuid(), $"user_{Guid.NewGuid():N}"[..20]))
            .ToList();

        var interactions = viewers
            .Select(v => new Interaction(v, art))
            .ToList();

        await SeedAsync(db =>
        {
            db.Users.Add(author);
            db.Users.AddRange(viewers);
            db.Arts.Add(art);
            db.Interactions.AddRange(interactions);
        });

        var startGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Act: 40 пользователей одновременно жмут кнопку "Лайк"
        var tasks = viewers.Select(viewer => Task.Run(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/arts/like?id={art.Id}");
            request.Headers.Add("X-User-Id", viewer.Id.ToString());

            await startGate.Task; // Ждем общего сигнала старта
            return await _client.SendAsync(request);
        })).ToArray();

        startGate.SetResult(); // Выстрел стартового пистолета!
        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));

        var actualLikesInInteractions = await GetFromDbAsync(db => 
            db.Interactions.CountAsync(i => i.ArtId == art.Id && i.Like));
        var artInDb = await GetFromDbAsync(db => 
            db.Arts.AsNoTracking().FirstAsync(a => a.Id == art.Id));

        // В таблице interactions будет ровно 40 лайков в обеих реализациях,
        // но на СТАРОМ коде artInDb.CountLikes окажется в районе 5..18 вместо 40!
        actualLikesInInteractions.Should().Be(ConcurrentUsersCount);
        artInDb.CountLikes.Should().Be(ConcurrentUsersCount);
    }

    [Fact]
    public async Task ViewArt_WhenViewedConcurrentlyBy40Users_ShouldNotLoseViews()
    {
        // Arrange: создаем автора, арт и 40 новых зрителей
        var author = new User(Guid.NewGuid(), $"author_{Guid.NewGuid():N}"[..20]);
        var art = new Art("Trending Artwork", "Desc", $"http://minio/{Guid.NewGuid():N}.jpg", false, author);
        
        var viewers = Enumerable.Range(0, ConcurrentUsersCount)
            .Select(_ => new User(Guid.NewGuid(), $"viewer_{Guid.NewGuid():N}"[..20]))
            .ToList();

        await SeedAsync(db =>
        {
            db.Users.Add(author);
            db.Users.AddRange(viewers);
            db.Arts.Add(art);
        });

        var startGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Act: 40 пользователей одновременно открывают арт
        var tasks = viewers.Select(viewer => Task.Run(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/arts/with-author?id={art.Id}");
            request.Headers.Add("X-User-Id", viewer.Id.ToString());

            await startGate.Task;
            return await _client.SendAsync(request);
        })).ToArray();

        startGate.SetResult();
        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));

        var actualViewsInInteractions = await GetFromDbAsync(db => 
            db.Interactions.CountAsync(i => i.ArtId == art.Id && i.View));
        var artInDb = await GetFromDbAsync(db => 
            db.Arts.AsNoTracking().FirstAsync(a => a.Id == art.Id));

        actualViewsInInteractions.Should().Be(ConcurrentUsersCount);
        artInDb.CountViews.Should().Be(ConcurrentUsersCount);
    }

    [Fact]
    public async Task Subscribe_WhenSubscribedConcurrentlyBy40Users_ShouldNotLoseSubscribers()
    {
        // Arrange: создаем популярного автора и 40 подписчиков
        var author = new User(Guid.NewGuid(), $"popular_{Guid.NewGuid():N}"[..20]);
        var subscribers = Enumerable.Range(0, ConcurrentUsersCount)
            .Select(_ => new User(Guid.NewGuid(), $"sub_{Guid.NewGuid():N}"[..20]))
            .ToList();

        await SeedAsync(db =>
        {
            db.Users.Add(author);
            db.Users.AddRange(subscribers);
        });

        var startGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Act: 40 человек одновременно подписываются на одного автора
        var tasks = subscribers.Select(sub => Task.Run(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/users/subscribe?id={author.Id}");
            request.Headers.Add("X-User-Id", sub.Id.ToString());

            await startGate.Task;
            return await _client.SendAsync(request);
        })).ToArray();

        startGate.SetResult();
        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));

        var actualSubsInTable = await GetFromDbAsync(db => 
            db.Subscriptions.CountAsync(s => s.TargetId == author.Id));
        var authorInDb = await GetFromDbAsync(db => 
            db.Users.AsNoTracking().FirstAsync(u => u.Id == author.Id));

        actualSubsInTable.Should().Be(ConcurrentUsersCount);
        authorInDb.CountSubscribers.Should().Be(ConcurrentUsersCount);
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