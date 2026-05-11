namespace PullUp.Application.Common.Authorization;

// Marker interface — a request that needs additional authorization beyond
// "authenticated user" should implement this AND register at least one
// IAuthorizationHandler<TRequest>.
public interface IAuthorizationRequirement
{
}
