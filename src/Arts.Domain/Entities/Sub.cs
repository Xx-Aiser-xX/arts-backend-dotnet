namespace Arts.Domain.Entities;

public class Sub : BaseEntity
{
    public DateTime SubscriptionDate { get; private init; }
    
    public Guid SubscriberId { get; private init; }
    public User Subscriber { get; private init; } = null!;

    public Guid TargetId { get; private init; }
    public User Target { get; private init; } = null!;

    protected Sub() { }

    public Sub(User subscriber, User target)
    {
        SubscriptionDate = DateTime.UtcNow;
        
        SubscriberId = subscriber.Id; 
        Subscriber = subscriber;
        
        TargetId = target.Id;
        Target = target;
    }
    
    public Sub(Guid subscriberId, Guid targetId)
    {
        SubscriptionDate = DateTime.UtcNow;
        SubscriberId = subscriberId;
        TargetId = targetId;
    }
}