using System.Security.Claims;
using Arts.Application.Interfaces;
using Arts.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Arts.Infrastructure.Services;

public class CurrentUserService(
    IHttpContextAccessor httpContextAccessor,
    IArtsDbContext dbContext) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var userClaims = httpContextAccessor.HttpContext?.User;
            
            var subClaim = userClaims?.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? userClaims?.FindFirstValue("sub");

            return Guid.TryParse(subClaim, out var userId) ? userId : null;
        }
    }
    
    public Guid GetRequiredUserId()
    {
        return UserId ?? throw new UnauthorizedAccessException("Пользователь не авторизован");
    }

    public bool IsAuthenticated => UserId.HasValue;

    public async Task<User?> GetCurrentUserAsync(CancellationToken ct = default)
    {
        if (UserId is null)
            return null;

        return await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == UserId.Value, ct);
    }

    public async Task<User> GetRequiredUserAsync(CancellationToken ct = default)
    {
        return await GetCurrentUserAsync(ct)
               ?? throw new UnauthorizedAccessException("Пользователь не авторизован или учетная запись не найдена");
    }
}