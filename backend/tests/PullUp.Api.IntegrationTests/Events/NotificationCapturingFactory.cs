using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PullUp.Application.Abstractions;

namespace PullUp.Api.IntegrationTests.Events;

public sealed class NotificationCapturingFactory : TestWebApplicationFactory
{
    public CapturingNotificationSender Notifications { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<INotificationSender>();
            services.AddSingleton<INotificationSender>(Notifications);
        });
    }
}
