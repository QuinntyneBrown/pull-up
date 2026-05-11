using MediatR;

namespace PullUp.Application.Features.Events.GetEvent;

public sealed record GetEventQuery(Guid Id) : IRequest<GetEventResponse>;
