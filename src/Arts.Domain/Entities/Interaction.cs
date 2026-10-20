namespace Arts.Domain.Entities;

public class Interaction : BaseEntity
{
    public bool Like { get; private set;}
    public DateTime? LikedAt { get; private set;}
    
    public bool View { get; private set;}
    public DateTime ViewAt { get; private set;}

    public Guid UserId { get; private init;}
    public User User { get; private init; } = null!;
    
    public Guid ArtId { get; private init;}
    public Art Art { get; private init; } = null!;

    protected Interaction() { }

    public Interaction(User user, Art art)
    {
        UserId = user.Id;
        User = user;
        
        ArtId = art.Id;
        Art = art;
        
        View = true;
        ViewAt = DateTime.UtcNow;
        Like = false;
    }
    
    public Interaction(Guid userId, Guid artId)
    {
        UserId = userId;
        ArtId = artId;
    
        View = true;
        ViewAt = DateTime.UtcNow;
        Like = false;
    }

    public void ToggleLike()
    {
        Like = !Like;
        LikedAt = Like ? DateTime.UtcNow : null;
    }

    public void UpdateViewTime()
    {
        ViewAt = DateTime.UtcNow;
    }
}