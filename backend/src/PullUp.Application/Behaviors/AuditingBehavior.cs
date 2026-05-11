using MediatR;
using PullUp.Application.Abstractions;
using PullUp.Application.Common.Auditing;

namespace PullUp.Application.Behaviors;

// Writes one AuditLogEntry per request that carries [AuditedAction("...")].
// Success path writes outcome=SUCCESS after the handler returns; failure path writes
// outcome=FAILURE on every exception thrown by the handler (and rethrows). Audit
// writes use a fresh DbContext scope inside IAuditLogger so they survive even when
// the handler aborts mid-operation.
public sealed class AuditingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserAccessor _currentUser;

    public AuditingBehavior(IAuditLogger auditLogger, ICurrentUserAccessor currentUser)
    {
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var attribute = (AuditedActionAttribute?)Attribute.GetCustomAttribute(
            request.GetType(), typeof(AuditedActionAttribute));

        if (attribute is null)
        {
            return await next(cancellationToken);
        }

        var correlationId = Guid.NewGuid();
        try
        {
            var response = await next(cancellationToken);

            await _auditLogger.WriteAsync(
                new AuditEntryDescriptor(
                    attribute.Event,
                    _currentUser.UserId,
                    DateTimeOffset.UtcNow,
                    correlationId,
                    "SUCCESS",
                    null),
                cancellationToken);

            return response;
        }
        catch
        {
            await _auditLogger.WriteAsync(
                new AuditEntryDescriptor(
                    attribute.Event,
                    _currentUser.UserId,
                    DateTimeOffset.UtcNow,
                    correlationId,
                    "FAILURE",
                    null),
                CancellationToken.None);
            throw;
        }
    }
}
