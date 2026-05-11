namespace PullUp.Application.Common.Auditing;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class AuditedActionAttribute : Attribute
{
    public AuditedActionAttribute(string @event)
    {
        ArgumentException.ThrowIfNullOrEmpty(@event);
        Event = @event;
    }

    public string Event { get; }
}
