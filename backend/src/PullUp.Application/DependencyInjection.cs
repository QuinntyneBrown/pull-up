using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PullUp.Application.Behaviors;
using PullUp.Application.Common.Authorization;

namespace PullUp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        // Pipeline behaviors run in registration order. Validation first (cheap, deterministic),
        // then authorization (may hit the DB), then the handler itself.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));

        // Discover every IAuthorizationHandler<T> in the Application assembly and register it
        // against its closed-generic interface so AuthorizationBehavior can take them as
        // IEnumerable<IAuthorizationHandler<TRequest>>.
        var authHandlerInterface = typeof(IAuthorizationHandler<>);
        foreach (var type in assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }))
        {
            var closed = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == authHandlerInterface);
            foreach (var i in closed)
            {
                services.AddTransient(i, type);
            }
        }

        return services;
    }
}
