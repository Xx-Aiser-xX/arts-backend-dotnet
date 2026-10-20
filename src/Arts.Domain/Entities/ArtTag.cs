namespace Arts.Domain.Entities;

public class ArtTag : BaseEntity
{
    public Guid ArtId { get; private init; }
    public Art Art { get; private init; } = null!;
    
    public Guid TagId { get; private init; }
    public Tag Tag { get; private init; } = null!;

    protected ArtTag() { }

    public ArtTag(Art art, Tag tag)
    {
        Art = art;
        ArtId = art.Id; 
        
        Tag = tag;
        TagId = tag.Id;
    }
}