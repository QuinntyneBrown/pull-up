using Microsoft.Extensions.DependencyInjection;
using PullUp.Application.Common.Auditing;
using PullUp.Domain.Audit;
using PullUp.Infrastructure.Persistence;

namespace PullUp.Infrastructure.Auditing;

// Writes audit rows in their own DbContext scope so partial state from a failed
// command handler is not flushed to the database when we record the FAILURE row.
public sealed class AuditLogger : IAuditLogger
{
    private readonly IServiceScopeFactory _scopeFactory;

    public AuditLogger(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task WriteAsync(AuditEntryDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var entry = AuditLogEntry.Record(
            descriptor.Event,
            descriptor.ActorUserId,
            descriptor.OccurredAt,
            descriptor.CorrelationId,
            descriptor.Outcome,
            descriptor.MetadataJson);

        db.AuditLog.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
    }
}
