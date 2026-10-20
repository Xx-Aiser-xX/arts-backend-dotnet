using Arts.Application.DTOs.Requests.Users;
using Arts.Application.DTOs.Responses;
using Arts.Application.DTOs.Responses.SocialNetworks;
using Arts.Application.DTOs.Responses.Users;
using Arts.Application.Interfaces;
using Arts.Domain.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Arts.Application.Services.Users;

public class UserService(
    IArtsDbContext dbContext,
    IFileStorage fileStorage,
    ICurrentUserService currentUserService,
    ILogger<UserService> logger) : IUserService
{
    public async Task<UserResponse> RegisterUserAsync(RegisterUserRequest request, CancellationToken ct = default)
    {
        var exist = await dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.UserName == request.UserName, ct);
        if (exist)
            throw new InvalidOperationException($"Пользователь с ником {request.UserName} уже существует");

        var user = new User(request.Id, request.UserName);
        var userPreferences = new UserPreferences(user);
        
        dbContext.Users.Add(user);
        dbContext.UserPreferences.Add(userPreferences);
        await dbContext.SaveChangesAsync(ct);
        
        logger.LogInformation("Зарегистрирован новый профиль пользователя '{UserName}' (ID: {UserId})", 
            user.UserName, user.Id);
        
        return user.Adapt<UserResponse>();
    }

    public async Task<UserResponse> UpdateUserAsync(UserUpdateRequest request, CancellationToken ct = default)
    {
        var user = await currentUserService.GetRequiredUserAsync(ct);
        if (!string.IsNullOrWhiteSpace(request.UserName) && request.UserName != user.UserName)
        {
            var exist = await dbContext.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.UserName == request.UserName, ct);
            if (exist)
                throw new InvalidOperationException($"Пользователь с ником {request.UserName} уже существует");
        }
        
        
        var photoUrl = user.PhotoUrl;
        if (request.AvatarFile is not null && request.AvatarFile.Length > 0)
        {
            photoUrl = await fileStorage.UploadFileAsync(request.AvatarFile, "avatars/", ct);
        }

        user.UpdateProfile(request.UserName, request.Description, photoUrl);

        if (request.SocialNetworks != null && request.SocialNetworks.Count != 0)
        {
            await SyncSocialNetworksAsync(user, request.SocialNetworks, ct);
        }

        await dbContext.SaveChangesAsync(ct);
        
        logger.LogInformation("Профиль пользователя {UserId} ('{UserName}') успешно обновлен", user.Id, user.UserName);

        return user.Adapt<UserResponse>();
    }
    
    private async Task SyncSocialNetworksAsync(User user, List<SocialNetworkRequest> newNetworks, CancellationToken ct)
    {
        var currentNetworks = await dbContext.SocialNetworks
            .IgnoreQueryFilters()
            .Where(s => s.UserId == user.Id)
            .ToListAsync(ct);

        var currentMap = currentNetworks.ToDictionary(s => s.Link, s => s);

        foreach (var req in newNetworks)
        {
            if (currentMap.TryGetValue(req.Link, out var existing))
            {
                if (existing.IsDeleted)
                    existing.Restore();
                currentMap.Remove(req.Link);
            }
            else
            {
                var newSn = new SocialNetwork(req.Link, user);
                dbContext.SocialNetworks.Add(newSn);
            }
        }

        foreach (var toDelete in currentMap.Values)
        {
            if (!toDelete.IsDeleted)
                toDelete.MarkAsDeleted();
        }
    }

    public async Task<Guid> DeleteAsync(CancellationToken ct = default)
    {
        var user = await currentUserService.GetRequiredUserAsync(ct);
        user.MarkAsDeleted();
        await dbContext.SaveChangesAsync(ct);
        
        logger.LogInformation("Пользователь {UserId} ('{UserName}') удалил свою учетную запись", user.Id, user.UserName);
        
        return user.Id;
    }

    public async Task<UserResponse> GetUserAsync(Guid id, CancellationToken ct = default)
    {
        return await dbContext.Users
                   .Where(u => u.Id == id)
                   .ProjectToType<UserResponse>()
                   .SingleOrDefaultAsync(ct)
               ?? throw new KeyNotFoundException($"Пользователь с ID {id} не найден");
    }

    public async Task<UserResponse> GetCurrentUserDtoAsync(CancellationToken ct = default)
    {
        var user = await currentUserService.GetRequiredUserAsync(ct);
        return user.Adapt<UserResponse>();
    }

    public async Task<UserMinResponse> GetCurrentUserMinDtoAsync(CancellationToken ct = default)
    {
        var user = await currentUserService.GetRequiredUserAsync(ct);
        return user.Adapt<UserMinResponse>();
    }

    public async Task<List<SocialNetworkResponse>> GetSocialNetworkUserAsync(Guid id, CancellationToken ct = default)
    {
        return await dbContext.SocialNetworks
            .Where(sn => sn.UserId == id)
            .ProjectToType<SocialNetworkResponse>()
            .ToListAsync(ct);
    }

    public async Task<SubscribeResponse> SubscribeAsync(Guid idAuthor, CancellationToken ct = default)
    {
        var curUserId = currentUserService.GetRequiredUserId();
        if (curUserId == idAuthor)
        {
            logger.LogWarning("Пользователь {UserId} попытался подписаться на самого себя", curUserId);
            throw new InvalidOperationException("Нельзя подписаться на самого себя");
        }

        var sub = await dbContext.Subscriptions
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(s => s.SubscriberId == curUserId && s.TargetId == idAuthor, ct);

        int delta;
        if (sub is null)
        {
            sub = new Sub(curUserId, idAuthor);
            dbContext.Subscriptions.Add(sub);
            delta = 1;
        }
        else if (sub.IsDeleted)
        {
            sub.Restore();
            delta = 1;
        }
        else
        {
            sub.MarkAsDeleted();
            delta = -1;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        var authorUpdated = await dbContext.Users
            .Where(u => u.Id == idAuthor)
            .ExecuteUpdateAsync(s => s.SetProperty(
                u => u.CountSubscribers,
                u => Math.Max(0, u.CountSubscribers + delta)), ct);

        if (authorUpdated == 0)
            throw new KeyNotFoundException("Автор не найден");

        var subscriberUpdated = await dbContext.Users
            .Where(u => u.Id == curUserId)
            .ExecuteUpdateAsync(s => s.SetProperty(
                u => u.CountSubscriptions,
                u => Math.Max(0, u.CountSubscriptions + delta)), ct);

        if (subscriberUpdated == 0)
            throw new UnauthorizedAccessException("Учетная запись подписчика не найдена");

        await dbContext.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        return sub.Adapt<SubscribeResponse>();
    }

    public async Task<bool> IsSubscribeAsync(Guid authorId, CancellationToken ct = default)
    {
        var curUserId = currentUserService.UserId;
        if (curUserId == null)
            return false;

        var isSubscribe = await dbContext.Subscriptions.AnyAsync(s => s.SubscriberId == curUserId && s.TargetId == authorId);

        return isSubscribe;
    }

    public async Task<PagedResponse<UserMinResponse>> GetSubscriptionsWithArtsAsync(int page, int size, CancellationToken ct = default)
    {
        var curUserId = currentUserService.GetRequiredUserId();

        var query = dbContext.Subscriptions
            .Where(s => s.SubscriberId == curUserId)
            .OrderByDescending(s => s.SubscriptionDate)
            .Select(s => s.Target);

        var totalElements = await query.CountAsync(ct);

        var authors = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ProjectToType<UserMinResponse>() 
            .ToListAsync(ct);

        return new PagedResponse<UserMinResponse>(authors, totalElements, page, size);
    }

    public async Task<List<UserMinResponse>> GetRecentSubscriptionsAsync(int limit, CancellationToken ct = default)
    {
        var curUserId = currentUserService.GetRequiredUserId();

        var recentAuthors = await dbContext.Subscriptions
            .Where(s => s.SubscriberId == curUserId)
            .OrderByDescending(s => s.SubscriptionDate)
            .Select(s => s.Target)
            .Take(limit)
            .ToListAsync(ct);

        return recentAuthors.Adapt<List<UserMinResponse>>();
    }
}