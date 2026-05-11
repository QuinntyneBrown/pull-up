using FluentValidation;

namespace PullUp.Application.Features.Events.UpdateEvent;

public sealed class UpdateEventCommandValidator : AbstractValidator<UpdateEventCommand>
{
    public UpdateEventCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(120);

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Location is required.")
            .MaximumLength(200);

        RuleFor(x => x.Description).MaximumLength(2000);

        RuleFor(x => x.EndsAtUtc)
            .Must((cmd, ends) => ends is null || ends > cmd.StartsAtUtc)
            .When(x => x.EndsAtUtc is not null)
            .WithMessage("Event end must be after the start.");
    }
}
