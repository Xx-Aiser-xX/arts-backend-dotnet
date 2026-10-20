namespace Arts.Domain.Entities;

public class Tag : BaseEntity
{
    public string Name {get; private init; }

    private readonly List<ArtTag> _artTags = [];
    public IReadOnlyCollection<ArtTag> ArtTags => _artTags.AsReadOnly();
    
    protected Tag() {}

    public Tag(string name)
    {
        ValidateName(name);
        Name = name;
    }
    
    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            throw new ArgumentException("Некорректный тег", nameof(name));
    }
}