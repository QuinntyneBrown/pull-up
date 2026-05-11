namespace PullUp.Application.Abstractions;

public interface ICurrentUserAccessor
{
    Guid? UserId { get; }
}
