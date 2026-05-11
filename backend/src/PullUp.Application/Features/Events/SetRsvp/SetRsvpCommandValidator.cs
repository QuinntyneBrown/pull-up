using FluentValidation;
using PullUp.Domain.Events;

namespace PullUp.Application.Features.Events.SetRsvp;

public sealed class SetRsvpCommandValidator : AbstractValidator<SetRsvpCommand>
{
    public SetRsvpCommandValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => Enum.TryParse<RsvpStatus>(s, ignoreCase: false, out _))
            .WithMessage("Status must be one of Going, Maybe, CantGo.");

        RuleFor(x => x.Note).MaximumLength(500);
    }
}
