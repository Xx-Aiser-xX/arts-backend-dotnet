namespace Arts.Domain.Entities;

public class UserPreferences : BaseEntity
{
    public Guid UserId { get; private init;}
    public User User { get; private init; } = null!;

    protected UserPreferences() { }

    public UserPreferences(User user)
    {
        UserId = user.Id;
        User = user;
    }
}