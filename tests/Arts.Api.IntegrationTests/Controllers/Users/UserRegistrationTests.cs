using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Arts.Api.IntegrationTests.Infrastructure;
using Arts.Application.DTOs.Requests;
using Arts.Application.DTOs.Requests.Users;
using Arts.Application.DTOs.Responses;
using Arts.Application.DTOs.Responses.Users;
using Arts.Domain.Entities;
using Arts.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arts.Api.IntegrationTests;

public class UserRegistrationTests(CustomWebApplicationFactory factory) 
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_WhenValidRequest_ShouldCreateUserAndPreferences()
    {
        var newUserId = Guid.NewGuid();
        var uniqueUserName = $"newbie_{Guid.NewGuid():N}"[..20];
        var requestDto = new RegisterUserRequest(newUserId, uniqueUserName);

        var content = new StringContent(JsonSerializer.Serialize(requestDto), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/users/register", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<UserResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(newUserId);
        result.UserName.Should().Be(uniqueUserName);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ArtsDbContext>();

        var userInDb = await db.Users.FirstOrDefaultAsync(u => u.Id == newUserId);
        userInDb.Should().NotBeNull();

        var preferencesInDb = await db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == newUserId);
        preferencesInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task Register_WhenUserNameAlreadyExists_ShouldReturnBadRequest()
    {
        var existingName = $"existing_{Guid.NewGuid():N}"[..20];
        var existingUser = new User(Guid.NewGuid(), existingName);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ArtsDbContext>();
            db.Users.Add(existingUser);
            await db.SaveChangesAsync();
        }

        var duplicateRequest = new RegisterUserRequest(Guid.NewGuid(), existingName);
        var content = new StringContent(JsonSerializer.Serialize(duplicateRequest), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/users/register", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetUser_WhenUserExists_ShouldReturnProfile()
    {
        var userId = Guid.NewGuid();
        var userName = $"profile_user_{Guid.NewGuid():N}"[..20];
        var user = new User(userId, userName);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ArtsDbContext>();
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/users?id={userId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<UserResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.UserName.Should().Be(userName);
    }
}