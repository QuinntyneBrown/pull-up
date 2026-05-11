using MediatR;
using PullUp.Application.Common.Authorization;
using PullUp.Application.Common.Exceptions;

namespace PullUp.Application.Behaviors;

// Runs every registered IAuthorizationHandler<TRequest> before the request handler.
// If any returns false, throws NotAuthorizedException (the API maps to HTTP 403).
// Commands that need authorization implement IAuthorizationRequirement (documentation
// marker) and register at least one IAuthorizationHandler<TCommand>; commands that do
// not register any handler are passed through unchanged.
public sealed class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IAuthorizationHandler<TRequest>> _handlers;

    public AuthorizationBehavior(IEnumerable<IAuthorizationHandler<TRequest>> handlers)
    {
        _handlers = handlers;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        foreach (var handler in _handlers)
        {
            var authorized = await handler.AuthorizeAsync(request, cancellationToken);
            if (!authorized)
            {
                throw new NotAuthorizedException();
            }
        }

        return await next(cancellationToken);
    }
}
