namespace Arts.Domain.Entities;

public class SocialNetwork : BaseEntity
{
    public string Link { get; private set; }
    
    public Guid UserId { get; private init; }
    public User User { get; private init; } = null!;

    protected SocialNetwork() { }

    public SocialNetwork(string link, User user)
    {
        ValidateLink(link);
        Link = link;
        
        UserId = user.Id;
        User = user;
    }
    
    public void UpdateLink(string newLink)
    {
        ValidateLink(newLink);
        Link = newLink;
    }
    
    private static void ValidateLink(string link)
    {
        if (string.IsNullOrWhiteSpace(link) || link.Length < 3 || link.Length > 2000)
            throw new ArgumentException("Некорректная ссылка", nameof(link));
    }
}