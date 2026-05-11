namespace PullUp.Application.Features.Events.GetEvent;

public sealed record HostSummary(Guid UserId, string FullName, string DisplayName, string Email);
