using Arts.Application.DTOs.Requests.Arts;
using Arts.Application.DTOs.Responses;
using Arts.Application.DTOs.Responses.Arts;
using Arts.Application.Exceptions;
using Arts.Application.Interfaces;
using Arts.Application.Services.Tags;
using Arts.Domain.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Arts.Application.Services.Arts;

public class ArtService(
    IArtsDbContext dbContext,
    IFileStorage fileStorage,
    ITaggerService taggerService,
    ITagDomainService  tagService,
    ICurrentUserService currentUserService,
    ILogger<ArtService> logger) : IArtService
{
    public async Task<ArtResponse> CreateAsync(ArtCreateRequest request, CancellationToken ct = default)
    {
        var user = await currentUserService.GetRequiredUserAsync(ct);

        var tagNames = request.Tags
            .Select(t => t.Name)
            .ToHashSet();

        try
        {
            var aiTags = await taggerService.GetTagsForImageAsync(request.ImageFile, ct);
            tagNames.UnionWith(aiTags);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось получить авто-теги от AI-сервиса для арта '{ArtName}'", request.Name);
        }

        string imageUrl = await fileStorage.UploadFileAsync(request.ImageFile, "arts/", ct);

        try
        {
            var art = new Art(request.Name, request.Description, imageUrl, request.IsNsfw, user);
            var tags = await tagService.GetOrCreateTagsAsync(tagNames, ct);
            var artTags = tags.Select(tag => new ArtTag(art, tag));

            dbContext.Arts.Add(art);
            dbContext.ArtTags.AddRange(artTags);

            await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

            await dbContext.SaveChangesAsync(ct);

            await dbContext.Users
                .Where(u => u.Id == user.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(
                    u => u.CountJobs,
                    u => u.CountJobs + 1), ct);

            await transaction.CommitAsync(ct);

            logger.LogInformation("Арт '{ArtName}' (ID: {ArtId}) успешно опубликован автором {UserId}. Привязано тегов: {TagCount}",
                art.Name, art.Id, user.Id, tags.Count);

            return art.Adapt<ArtResponse>();
        }
        catch
        {
            try
            {
                await fileStorage.DeleteFileAsync(imageUrl, CancellationToken.None);
            }
            catch (Exception cleanupEx)
            {
                logger.LogError(cleanupEx, "Не удалось удалить файл-сироту {ImageUrl} из MinIO после отката БД", imageUrl);
            }
            throw;
        }
    }
    
    
    public async Task<ArtResponse> UpdateAsync(ArtUpdateRequest request, CancellationToken ct = default)
    {
        var currentUserId = currentUserService.GetRequiredUserId();

        var art = await dbContext.Arts
                      .Include(a => a.Author)
                      .FirstOrDefaultAsync(a => a.Id == request.Id, ct)
                  ?? throw new KeyNotFoundException("Арт не найден");

        if (art.AuthorId != currentUserId)
        {
            logger.LogWarning("Пользователь {UserId} попытался отредактировать чужой арт {ArtId} (Владелец: {OwnerId})", 
                currentUserId, art.Id, art.AuthorId);
            throw new ForbiddenException("Вы не являетесь владельцем Арта");
        }

        art.UpdateDetails(request.Name, request.Description, request.IsNsfw);
        await dbContext.SaveChangesAsync(ct);
        
        logger.LogInformation("Арт {ArtId} успешно обновлен автором {UserId}", art.Id, currentUserId);

        return art.Adapt<ArtResponse>();
    }
    
    public async Task<ArtResponse> GetArtAndAuthorByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await dbContext.Arts
                   .Where(a => a.Id == id)
                   .ProjectToType<ArtResponse>() 
                   .SingleOrDefaultAsync(ct)
               ?? throw new KeyNotFoundException("Арт не найден");
    }

    public async Task<Guid> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var currentUserId = currentUserService.GetRequiredUserId();

        var art = await dbContext.Arts.FirstOrDefaultAsync(a => a.Id == id, ct)
                  ?? throw new KeyNotFoundException("Арт не найден");

        if (art.AuthorId != currentUserId)
        {
            logger.LogWarning("Пользователь {UserId} попытался удалить чужой арт {ArtId}", currentUserId, art.Id);
            throw new ForbiddenException("Вы не являетесь владельцем Арта");
        }

        art.MarkAsDeleted();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        await dbContext.SaveChangesAsync(ct);

        await dbContext.Users
            .Where(u => u.Id == currentUserId)
            .ExecuteUpdateAsync(s => s.SetProperty(
                u => u.CountJobs,
                u => Math.Max(0, u.CountJobs - 1)), ct);

        await transaction.CommitAsync(ct);

        logger.LogInformation("Арт {ArtId} ('{ArtName}') был мягко удален автором {UserId}", art.Id, art.Name, currentUserId);
        
        return art.AuthorId;
    }
    
    public async Task<bool> LikeArtAsync(Guid id, CancellationToken ct = default)
    {
        var curUserId = currentUserService.GetRequiredUserId();

        var interaction = await dbContext.Interactions
                              .FirstOrDefaultAsync(i => i.UserId == curUserId && i.ArtId == id, ct)
                          ?? throw new InvalidOperationException("Невозможно поставить лайк записи, которую вы не просматривали");

        interaction.ToggleLike();
        var delta = interaction.Like ? 1 : -1;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        await dbContext.SaveChangesAsync(ct);

        var updatedRows = await dbContext.Arts
            .Where(a => a.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(
                a => a.CountLikes, 
                a => Math.Max(0, a.CountLikes + delta)), ct);

        if (updatedRows == 0)
            throw new KeyNotFoundException("Арт не найден");

        await transaction.CommitAsync(ct);

        return interaction.Like;
    }

    public async Task<bool> IsLikeArtAsync(Guid? id, CancellationToken ct = default)
    {
        var curUserId = currentUserService.UserId;
        
        if (curUserId == null)
            return false;

        return await dbContext.Interactions
            .AnyAsync(i => i.UserId == curUserId.Value && i.ArtId == id && i.Like, ct);
    }

    public async Task ViewArtAsync(Guid artId, CancellationToken ct = default)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
            return;
    
        var alreadyViewed = await dbContext.Interactions
            .AnyAsync(i => i.UserId == userId.Value && i.ArtId == artId, ct);
    
        if (alreadyViewed)
            return;
    
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
    
        try
        {
            var updatedRows = await dbContext.Arts
                .Where(a => a.Id == artId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.CountViews, x => x.CountViews + 1), ct);
    
            if (updatedRows == 0)
                throw new KeyNotFoundException("Арт не найден");
    
            var interaction = new Interaction(userId.Value, artId);
            dbContext.Interactions.Add(interaction);
            await dbContext.SaveChangesAsync(ct);
    
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
        }
    }
    
    public async Task<PagedResponse<ArtCardResponse>> GetLikedArtsByCurrentUserAsync(int page, int size, CancellationToken ct = default)
    {
        var curUserId = currentUserService.GetRequiredUserId();

        var query = dbContext.Interactions
            .Where(i => i.UserId == curUserId && i.Like)
            .OrderByDescending(i => i.LikedAt)
            .Select(i => i.Art);

        return await PaginateAndMapAsync(query, page, size, ct);
    }

    public async Task<PagedResponse<ArtCardResponse>> GetArtByAuthorIdAsync(Guid authorId, int page, int size, CancellationToken ct = default)
    {
        var query = dbContext.Arts
            .Where(a => a.AuthorId == authorId)
            .OrderByDescending(a => a.PublicationTime);

        return await PaginateAndMapAsync(query, page, size, ct);
    }

    public async Task<PagedResponse<ArtCardResponse>> SearchArtsByTagNameAndNameAsync(string query, int page, int size, CancellationToken ct = default)
    {
        
        logger.LogDebug("Поиск артов по запросу '{Query}', страница {Page}", query, page);
        
        if (string.IsNullOrWhiteSpace(query))
        {
            var emptyQuery = dbContext.Arts.OrderByDescending(a => a.PublicationTime);
            return await PaginateAndMapAsync(emptyQuery, page, size, ct);
        }

        var normalizedQuery = query.Trim().ToLower();

        var searchQuery = dbContext.Arts
            .Where(a => a.Name.ToLower().Contains(normalizedQuery)
                        || a.ArtTags.Any(at => at.Tag.Name.ToLower().Contains(normalizedQuery)));

        var prioritizedQuery = searchQuery
            .OrderBy(a => a.ArtTags.Any(at => at.Tag.Name.ToLower().Contains(normalizedQuery)) ? 0 : 1)
            .ThenByDescending(a => a.PublicationTime);

        return await PaginateAndMapAsync(prioritizedQuery, page, size, ct);
    }

    public async Task<PagedResponse<ArtCardResponse>> GetFeedAsync(string type, int page, int size, CancellationToken ct = default)
    {
        logger.LogDebug("Запрос ленты '{FeedType}', страница {Page}", type, page);
        
        var query = type.ToLower() switch
        {
            "trending" => GetTrendingQuery(),
            "recommended" => GetRecommendedQuery(),
            "subscriptions" => GetSubscriptionsQuery(),
            "latest" => GetLatestQuery(),
            _ => throw new ArgumentException($"Неправильный тип сортировки артов: {type}")
        };

        return await PaginateAndMapAsync(query, page, size, ct);
    }

    private IQueryable<Art> GetTrendingQuery()
    {
        var cutoff = DateTime.UtcNow.AddDays(-1000);
        
        return dbContext.Arts
            .OrderByDescending(a => a.Interactions.Count(i => i.Like && i.LikedAt >= cutoff))
            .ThenByDescending(a => a.PublicationTime);
    }

    private IQueryable<Art> GetLatestQuery()
    {
        var cutoff = DateTime.UtcNow.AddDays(-60);
        
        return dbContext.Arts
            .Where(a => a.PublicationTime >= cutoff)
            .OrderByDescending(a => a.PublicationTime);
    }

    private IQueryable<Art> GetSubscriptionsQuery()
    {
        var userId = currentUserService.GetRequiredUserId();
        
        return dbContext.Arts
            .Where(a => dbContext.Subscriptions.Any(s => s.SubscriberId == userId && s.TargetId == a.AuthorId))
            .OrderByDescending(a => a.PublicationTime);
    }


    private IQueryable<Art> GetRecommendedQuery()
    {
        var userId = currentUserService.GetRequiredUserId();
        
        var userInteractedTagIds = dbContext.Interactions
            .Where(i => i.UserId == userId)
            .SelectMany(i => i.Art.ArtTags.Select(at => at.TagId));

        return dbContext.Arts
            .Where(a => a.ArtTags.Any(at => userInteractedTagIds.Contains(at.TagId)))
            .OrderByDescending(a => a.PublicationTime);
    }

    private static async Task<PagedResponse<ArtCardResponse>> PaginateAndMapAsync(IQueryable<Art> query, int page, int size, CancellationToken ct)
    {
        var totalElements = await query.CountAsync(ct);

        var arts = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ProjectToType<ArtCardResponse>() 
            .ToListAsync(ct);

        return new PagedResponse<ArtCardResponse>(arts, totalElements, page, size);
    }
}