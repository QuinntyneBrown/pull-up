namespace PullUp.Application.Common.Auditing;

public interface IAuditLogger
{
    Task WriteAsync(AuditEntryDescriptor descriptor, CancellationToken cancellationToken = default);
}
