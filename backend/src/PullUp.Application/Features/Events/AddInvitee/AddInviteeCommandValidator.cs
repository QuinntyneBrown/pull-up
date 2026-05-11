using FluentValidation;

namespace PullUp.Application.Features.Events.AddInvitee;

public sealed class AddInviteeCommandValidator : AbstractValidator<AddInviteeCommand>
{
    public AddInviteeCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Enter a valid email address.")
            .MaximumLength(254);
    }
}
