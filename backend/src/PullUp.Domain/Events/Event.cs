namespace PullUp.Domain.Events;

public sealed class Event
{
    public Guid Id { get; private set; }
    public Guid HostId { get; private set; }
    public string Title { get; private set; } = null!;
    public DateTimeOffset StartsAtUtc { get; private set; }
    public DateTimeOffset? EndsAtUtc { get; private set; }
    public string Location { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public bool AllowPlusOne { get; private set; }
    public bool ShowGuestList { get; private set; }
    public EventStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Event() { }

    public static Event Create(
        Guid hostId,
        string title,
        DateTimeOffset startsAtUtc,
        DateTimeOffset? endsAtUtc,
        string location,
        string description,
        bool allowPlusOne,
        bool showGuestList,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);
        ArgumentException.ThrowIfNullOrEmpty(location);
        return new Event
        {
            Id = Guid.NewGuid(),
            HostId = hostId,
            Title = title.Trim(),
            StartsAtUtc = startsAtUtc,
            EndsAtUtc = endsAtUtc,
            Location = location.Trim(),
            Description = description?.Trim() ?? string.Empty,
            AllowPlusOne = allowPlusOne,
            ShowGuestList = showGuestList,
            Status = EventStatus.Scheduled,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void UpdateDetails(
        string title,
        DateTimeOffset startsAtUtc,
        DateTimeOffset? endsAtUtc,
        string location,
        string description,
        bool allowPlusOne,
        bool showGuestList,
        DateTimeOffset now)
    {
        Title = title.Trim();
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        Location = location.Trim();
        Description = description?.Trim() ?? string.Empty;
        AllowPlusOne = allowPlusOne;
        ShowGuestList = showGuestList;
        UpdatedAt = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status == EventStatus.Cancelled) return;
        Status = EventStatus.Cancelled;
        UpdatedAt = now;
    }
}
