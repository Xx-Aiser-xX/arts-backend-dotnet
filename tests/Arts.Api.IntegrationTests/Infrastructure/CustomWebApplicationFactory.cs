using Arts.Application.Interfaces;
using Arts.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Testcontainers.PostgreSql;

namespace Arts.Api.IntegrationTests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("test_arts")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ArtsDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ArtsDbContext>));
            if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);

            services.AddDbContextPool<ArtsDbContext>(options =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });

            var cacheDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICacheService));
            if (cacheDescriptor != null) services.Remove(cacheDescriptor);
            
            services.AddScoped<ICacheService, TestCacheService>();

            var fileStorageDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IFileStorage));
            if (fileStorageDescriptor != null) services.Remove(fileStorageDescriptor);
            services.AddScoped(_ => Substitute.For<IFileStorage>());

            var taggerDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITaggerService));
            if (taggerDescriptor != null) services.Remove(taggerDescriptor);
            services.AddScoped(_ => Substitute.For<ITaggerService>());

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.AuthenticationScheme, _ => { });
        });
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}

public class TestCacheService : ICacheService
{
    public Task<T> GetOrCreateAsync<T>(string key, string policyName, Func<Task<T>> factory, CancellationToken ct = default) 
        => factory();

    public Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken ct = default) 
        => factory();

    public Task RemoveAsync(string key, CancellationToken ct = default) 
        => Task.CompletedTask;

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default) 
        => Task.CompletedTask;
}