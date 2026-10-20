using System.Security.Claims;
using Amazon.S3;
using Arts.Api.Middlewares;
using Arts.Application.Interfaces;
using Arts.Application.Mappings;
using Arts.Application.Services.Arts;
using Arts.Application.Services.Comments;
using Arts.Application.Services.Tags;
using Arts.Application.Services.Users;
using Arts.Infrastructure.Caching;
using Arts.Infrastructure.Caching.Decorators;
using Arts.Infrastructure.Configuration;
using Arts.Infrastructure.Persistence;
using Arts.Infrastructure.Services;
using Arts.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

MapsterConfig.RegisterMappings();

builder.Services.Configure<MinioOptions>(builder.Configuration.GetSection(MinioOptions.SectionName));
builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetSection(KeycloakOptions.SectionName));
builder.Services.Configure<TaggerOptions>(builder.Configuration.GetSection(TaggerOptions.SectionName));
builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection(CacheSettings.SectionName));

var keycloakOptions = builder.Configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>() 
    ?? throw new InvalidOperationException("Секция Keycloak не настроена в конфигурации");

var taggerOptions = builder.Configuration.GetSection(TaggerOptions.SectionName).Get<TaggerOptions>()
    ?? throw new InvalidOperationException("Секция Tagger не настроена в конфигурации");

builder.Services.AddDbContextPool<ArtsDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddScoped<IArtsDbContext>(sp => sp.GetRequiredService<ArtsDbContext>());

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    // options.InstanceName = "ArtsApp:";
});

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var redisConnString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    var config = ConfigurationOptions.Parse(redisConnString);
    config.AbortOnConnectFail = false; 
    return ConnectionMultiplexer.Connect(config);
});

builder.Services.AddScoped<ICacheService, RedisCacheService>();

builder.Services.AddMemoryCache();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = keycloakOptions.RealmUrl;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = keycloakOptions.RealmUrl,
            ValidateAudience = false,
            ValidateLifetime = true,
            NameClaimType = ClaimTypes.NameIdentifier
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpClient<IKeycloakService, KeycloakService>();
builder.Services.AddHttpClient<ITaggerService, TaggerService>(client =>
{
    client.BaseAddress = new Uri(taggerOptions.BaseUrl);
});



builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.Decorate<ICommentService, CachedCommentService>();

builder.Services.AddScoped<ITagService, TagService>();
builder.Services.Decorate<ITagService, CachedTagService>();
builder.Services.AddScoped<ITagDomainService, TagService>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.Decorate<IUserService, CachedUserService>();

builder.Services.AddScoped<IArtService, ArtService>();
builder.Services.Decorate<IArtService, CachedArtService>();

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var minioOpts = sp.GetRequiredService<IOptions<MinioOptions>>().Value;
    var config = new AmazonS3Config
    {
        ServiceURL = minioOpts.EndpointUrl,
        ForcePathStyle = true
    };
    return new AmazonS3Client(minioOpts.AccessKey, minioOpts.SecretKey, config);
});
builder.Services.AddScoped<IFileStorage, MinioFileStorage>();



builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();



var app = builder.Build();

app.UseExceptionHandler();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }