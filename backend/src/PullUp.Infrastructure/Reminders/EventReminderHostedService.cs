using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PullUp.Application.Features.Events.DispatchEventReminders;

namespace PullUp.Infrastructure.Reminders;

// Runs once per minute. Each tick invokes DispatchEventRemindersCommand which
// finds Rsvps in the 24h reminder window and fans out one EventReminder per
// recipient (gated by their NotificationPreference.EventReminders).
public sealed class EventReminderHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EventReminderHostedService> _logger;

    public EventReminderHostedService(IServiceScopeFactory scopeFactory, ILogger<EventReminderHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
                await mediator.Send(new DispatchEventRemindersCommand(), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Event reminder dispatch failed; continuing next tick.");
            }
        }
    }
}
