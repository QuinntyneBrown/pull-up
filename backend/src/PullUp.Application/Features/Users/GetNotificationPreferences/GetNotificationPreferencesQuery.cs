using MediatR;

namespace PullUp.Application.Features.Users.GetNotificationPreferences;

public sealed record GetNotificationPreferencesQuery() : IRequest<NotificationPreferencesResponse>;
