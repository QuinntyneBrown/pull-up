using FluentValidation;

namespace PullUp.Application.Features.Events.CreateEvent;

public sealed class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(120).WithMessage("Title cannot exceed 120 characters.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Location is required.")
            .MaximumLength(200).WithMessage("Location cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");

        RuleFor(x => x.StartsAtUtc)
            .Must(d => d >= DateTimeOffset.UtcNow.Date)
            .WithMessage("Event date must be today or later.");

        RuleFor(x => x.EndsAtUtc)
            .Must((cmd, ends) => ends is null || ends > cmd.StartsAtUtc)
            .When(x => x.EndsAtUtc is not null)
            .WithMessage("Event end must be after the start.");

        RuleForEach(x => x.InviteeEmails)
            .NotEmpty().WithMessage("Invitee email cannot be empty.")
            .EmailAddress().WithMessage("Invitee email must be a valid address.")
            .MaximumLength(254);
    }
}
