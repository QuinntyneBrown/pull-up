using MediatR;

namespace PullUp.Application.Features.Events.DispatchEventReminders;

// Fires once per minute from EventReminderHostedService. Idempotent: a Rsvp
// row with ReminderSentAt set is skipped, so restarts cannot send duplicates.
public sealed record DispatchEventRemindersCommand : IRequest<Unit>;
