using MediatR;

namespace PullUp.Application.Features.Events.ListMyEvents;

public sealed record ListMyEventsQuery(string? Scope) : IRequest<ListMyEventsResponse>;
