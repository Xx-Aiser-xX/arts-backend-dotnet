namespace Arts.Domain.Entities;

public class Art : BaseEntity
{
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string ImageUrl { get; private set; }
    
    public int CountLikes { get; private set; }
    public int CountViews { get; private set; }
    public bool IsNsfw { get; private set; }
    public DateTime PublicationTime { get; private init; }

    public Guid AuthorId { get; private init; }
    public User Author { get; private init; } = null!;

    private readonly List<ArtTag> _artTags = [];
    public IReadOnlyCollection<ArtTag> ArtTags => _artTags.AsReadOnly();

    private readonly List<Comment> _comments = [];
    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();
    
    private readonly List<Interaction> _interactions = [];
    public IReadOnlyCollection<Interaction> Interactions => _interactions.AsReadOnly();

    protected Art() { }

    public Art(string name, string? description, string imageUrl, bool isNsfw, User author)
    {
        ValidateName(name);
        ValidateDescription(description);
        ValidateImageUrl(imageUrl);

        Name = name;
        Description = description;
        ImageUrl = imageUrl;
        IsNsfw = isNsfw;
        AuthorId = author.Id;
        Author = author;
        
        CountLikes = 0;
        CountViews = 0;
        PublicationTime = DateTime.UtcNow;
    }

    public void IncrementViews() => CountViews++;
    public void IncrementLikes() => CountLikes++;
    public void DecrementLikes() => CountLikes = Math.Max(0, CountLikes - 1);
    
    public void UpdateDetails(string name, string? description, bool isNsfw)
    {
        ValidateName(name);
        ValidateDescription(description);
        Name = name;
        Description = description;
        IsNsfw = isNsfw;
    }
    
    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length < 6 || name.Length > 200)
            throw new ArgumentException("Некорректное название", nameof(name));
    }

    private static void ValidateDescription(string? description)
    {
        if (description != null && description.Length > 5000)
            throw new ArgumentException("Описание слишком длинное", nameof(description));
    }

    private static void ValidateImageUrl(string? photoUrl)
    {
        if (photoUrl != null && photoUrl.Length > 2000)
            throw new ArgumentException("URL фото слишком длинный", nameof(photoUrl));
    }
}