using MediatR;
using PullUp.Application.Common.Auditing;

namespace PullUp.Application.Features.Users.UpdateProfile;

[AuditedAction("USER_PROFILE_UPDATED")]
public sealed record UpdateProfileCommand(string FullName, string DisplayName) : IRequest<Unit>;
