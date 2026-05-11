using MediatR;
using PullUp.Application.Common.Authorization;

namespace PullUp.Api.IntegrationTests.Behaviors.Fakes;

public sealed record FakeAuthRequest : IRequest<Unit>, IAuthorizationRequirement;
