using MediatR;

namespace PullUp.Application.Features.Users.GetCurrentUser;

public sealed record GetCurrentUserQuery() : IRequest<GetCurrentUserResponse>;
