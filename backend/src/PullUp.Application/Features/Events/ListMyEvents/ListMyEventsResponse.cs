namespace PullUp.Application.Features.Events.ListMyEvents;

public sealed record ListMyEventsResponse(
    IReadOnlyList<EventSummary> ThisWeek,
    IReadOnlyList<EventSummary> LaterThisMonth,
    IReadOnlyList<EventSummary> NextMonth,
    IReadOnlyList<EventSummary> Past);
