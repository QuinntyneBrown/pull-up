namespace PullUp.Application.Features.Events.SetRsvp;

public sealed class EventAlreadyPassedException : Exception
{
    public EventAlreadyPassedException() : base("This event has already started and RSVPs can no longer be set.")
    {
    }
}
