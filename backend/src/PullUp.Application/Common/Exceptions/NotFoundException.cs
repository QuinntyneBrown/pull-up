namespace PullUp.Application.Common.Exceptions;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string entity, Guid id)
        : base($"{entity} with id '{id}' was not found.")
    {
        Entity = entity;
        Id = id;
    }

    public string Entity { get; }
    public Guid Id { get; }
}
