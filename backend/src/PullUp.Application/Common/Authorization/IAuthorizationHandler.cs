namespace PullUp.Application.Common.Authorization;

// Per-request authorization rule. Each command that needs entity-level
// authorization (e.g., host-only) provides a colocated handler in the same
// feature folder. The TRequest type is unconstrained at the type-system level
// so the open-generic AuthorizationBehavior pipeline can resolve handlers
// without enforcing the marker; convention is that TRequest implements
// IAuthorizationRequirement to document intent at the call site.
public interface IAuthorizationHandler<in TRequest>
{
    Task<bool> AuthorizeAsync(TRequest request, CancellationToken cancellationToken);
}
