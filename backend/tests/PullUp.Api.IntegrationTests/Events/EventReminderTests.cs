// Acceptance Test
// Traces to: L2-038 (24-hour reminder, gated on EventReminders preference;
// idempotent across restarts).

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PullUp.Application.Abstractions;
using PullUp.Application.Features.Events.DispatchEventReminders;
using PullUp.Domain.Events;
using PullUp.Domain.Notifications;
using PullUp.Domain.Users;
using PullUp.Infrastructure.Persistence;
using PullUp.Infrastructure.Security;
using Xunit;

namespace PullUp.Api.IntegrationTests.Events;

public sealed class EventReminderTests : IClassFixture<NotificationCapturingFactory>
{
    private readonly NotificationCapturingFactory _factory;

    public EventReminderTests(NotificationCapturingFactory factory)
    {
        _factory = factory;
        _factory.EnsureDatabaseCreated();
    }

    private async Task<Guid> SeedUserAsync(AppDbContext db, string emailSuffix, bool reminderPreferenceOn)
    {
        var user = User.Register(
            email: $"reminder.{emailSuffix}.{Guid.NewGuid():N}@example.com",
            fullName: $"Reminder {emailSuffix}",
            passwordHash: new Pbkdf2PasswordHasher().Hash("Hunter2!secret"));
        db.Users.Add(user);
        db.NotificationPreferences.Add(reminderPreferenceOn
            ? NotificationPreference.DefaultFor(user.Id)
            : Build(user.Id, eventReminders: false));
        await db.SaveChangesAsync();
        return user.Id;

        static NotificationPreference Build(Guid uid, bool eventReminders)
        {
            var p = NotificationPreference.DefaultFor(uid);
            p.Update(newInvitations: true, eventReminders: eventReminders, rsvpChanges: true);
            return p;
        }
    }

    [Fact]
    public async Task Dispatcher_sends_reminder_to_attending_users_with_reminders_on_and_is_idempotent()
    {
        var dispatcher = _factory.Services.GetRequiredService<ISender>();

        Guid eventId;
        Guid attendingOnId;
        Guid attendingOffId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var hostId = await SeedUserAsync(db, "host", reminderPreferenceOn: true);
            attendingOnId = await SeedUserAsync(db, "on", reminderPreferenceOn: true);
            attendingOffId = await SeedUserAsync(db, "off", reminderPreferenceOn: false);

            var ev = Event.Create(
                hostId: hostId,
                title: "Reminder Test",
                startsAtUtc: DateTimeOffset.UtcNow.AddHours(24).AddMinutes(5),
                endsAtUtc: null,
                location: "Home",
                description: "",
                allowPlusOne: true,
                showGuestList: true,
                now: DateTimeOffset.UtcNow);
            db.Events.Add(ev);
            db.Rsvps.Add(Rsvp.Create(ev.Id, attendingOnId, RsvpStatus.Going, null, DateTimeOffset.UtcNow));
            db.Rsvps.Add(Rsvp.Create(ev.Id, attendingOffId, RsvpStatus.Going, null, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
            eventId = ev.Id;
        }

        _factory.Notifications.Sent.Clear();
        await dispatcher.Send(new DispatchEventRemindersCommand());

        Assert.Contains(
            _factory.Notifications.Sent,
            n => n.RecipientUserId == attendingOnId
                 && n.Kind == NotificationKind.EventReminder
                 && n.EventId == eventId);
        Assert.DoesNotContain(
            _factory.Notifications.Sent,
            n => n.RecipientUserId == attendingOffId);

        // Idempotent: a second run produces no further reminders for the same RSVPs.
        var beforeSecond = _factory.Notifications.Sent.Count;
        await dispatcher.Send(new DispatchEventRemindersCommand());
        Assert.Equal(beforeSecond, _factory.Notifications.Sent.Count);
    }
}
