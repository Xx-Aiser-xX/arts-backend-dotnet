namespace Arts.Domain.Entities;

public class Comment : BaseEntity
{
    public string Text { get; private set; }
    public DateTime PublicationTime { get; private init; }

    public Guid ArtId { get; private init; }
    public Art Art { get; private init; } = null!;

    public Guid AuthorId { get; private init; }
    public User Author { get; private init; } = null!;

    protected Comment() { }

    public Comment(string text, Art art, User author)
    {
        ValidateText(text);
        Text = text;
        
        ArtId = art.Id;
        Art = art;
        
        AuthorId = author.Id;
        Author = author;
        
        PublicationTime = DateTime.UtcNow;
    }

    public void UpdateDetails(string text)
    {
        ValidateText(text);
        Text = text;
    }

    private static void ValidateText(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 200)
            throw new ArgumentException("Комментарий слишком длинный", nameof(text));
    }
}