using MediatR;
using PullUp.Application.Common.Auditing;

namespace PullUp.Application.Features.Users.RefreshAccessToken;

[AuditedAction("ACCESS_TOKEN_REFRESHED")]
public sealed record RefreshAccessTokenCommand(string RefreshToken) : IRequest<RefreshAccessTokenResponse>;
