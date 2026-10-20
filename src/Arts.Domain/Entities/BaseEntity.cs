namespace Arts.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; protected init; } = Guid.NewGuid();
    public bool IsDeleted { get; private set; } = false;

    public void MarkAsDeleted() => IsDeleted = true;
    public void Restore() => IsDeleted = false;
}