using Arts.Application.DTOs.Requests.Users;
using Arts.Application.DTOs.Responses;
using Arts.Application.DTOs.Responses.SocialNetworks;
using Arts.Application.DTOs.Responses.Users;

namespace Arts.Application.Services.Users;

public interface IUserService
{
    Task<UserResponse> RegisterUserAsync(RegisterUserRequest request, CancellationToken ct = default);
    Task<UserResponse> UpdateUserAsync(UserUpdateRequest request, CancellationToken ct = default);
    Task<Guid> DeleteAsync(CancellationToken ct = default);

    Task<UserResponse> GetUserAsync(Guid id, CancellationToken ct = default);
    
    Task<UserResponse> GetCurrentUserDtoAsync(CancellationToken ct = default);
    Task<UserMinResponse> GetCurrentUserMinDtoAsync(CancellationToken ct = default);
    
    Task<List<SocialNetworkResponse>> GetSocialNetworkUserAsync(Guid id, CancellationToken ct = default);
    
    Task<SubscribeResponse> SubscribeAsync(Guid idAuthor, CancellationToken ct = default);
    Task<bool> IsSubscribeAsync(Guid authorId, CancellationToken ct = default);
    
    Task<PagedResponse<UserMinResponse>> GetSubscriptionsWithArtsAsync(int page, int size, CancellationToken ct = default);
    
    Task<List<UserMinResponse>> GetRecentSubscriptionsAsync(int limit, CancellationToken ct = default);
}