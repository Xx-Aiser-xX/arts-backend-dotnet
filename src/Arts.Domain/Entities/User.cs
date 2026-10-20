namespace Arts.Domain.Entities;

public class User : BaseEntity
{
    public string UserName { get; private set; }
    public string? Description { get; private set; }
    public string? PhotoUrl { get; private set; }
    
    public int CountJobs { get; private set; }
    public int CountSubscriptions { get; private set; }
    public int CountSubscribers { get; private set; }

    private readonly List<Art> _arts = [];
    public IReadOnlyCollection<Art> Arts => _arts.AsReadOnly();
    
    private readonly List<Interaction> _interactions = [];
    public IReadOnlyCollection<Interaction> Interactions => _interactions.AsReadOnly();
    
    private readonly List<SocialNetwork> _socialNetworks = [];
    public IReadOnlyCollection<SocialNetwork> SocialNetworks => _socialNetworks.AsReadOnly();

    private readonly List<Sub> _subscribers = [];
    public IReadOnlyCollection<Sub> Subscribers => _subscribers.AsReadOnly();

    private readonly List<Sub> _targets = [];
    public IReadOnlyCollection<Sub> Targets => _targets.AsReadOnly();
    
    protected User() { }

    public User(Guid id, string userName)
    {
        Id = id;
        ValidateUserName(userName);
        UserName = userName;
        
        CountJobs = 0;
        CountSubscriptions = 0;
        CountSubscribers = 0;
    }

    public void UpdateProfile(string userName, string? description, string? photoUrl)
    {
        ValidateUserName(userName);
        ValidateDescription(description);
        ValidatePhotoUrl(photoUrl);

        UserName = userName;
        Description = description;
        PhotoUrl = photoUrl;
    }

    public void IncrementJobs() => CountJobs++;
    public void DecrementJobs() => CountJobs = Math.Max(0, CountJobs - 1);
    
    public void IncrementSubscribers() => CountSubscribers++;
    public void DecrementSubscribers() => CountSubscribers = Math.Max(0, CountSubscribers - 1);
    
    public void IncrementSubscriptions() => CountSubscriptions++;
    public void DecrementSubscriptions() => CountSubscriptions = Math.Max(0, CountSubscriptions - 1);

    
    private static void ValidateUserName(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName) || userName.Length < 2 || userName.Length > 60)
            throw new ArgumentException("Некорректное имя пользователя", nameof(userName));
    }

    private static void ValidateDescription(string? description)
    {
        if (description != null && description.Length > 5000)
            throw new ArgumentException("Описание слишком длинное", nameof(description));
    }

    private static void ValidatePhotoUrl(string? photoUrl)
    {
        if (photoUrl != null && photoUrl.Length > 2000)
            throw new ArgumentException("URL фото слишком длинный", nameof(photoUrl));
    }
}