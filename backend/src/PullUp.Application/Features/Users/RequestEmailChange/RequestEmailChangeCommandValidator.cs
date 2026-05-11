using FluentValidation;

namespace PullUp.Application.Features.Users.RequestEmailChange;

public sealed class RequestEmailChangeCommandValidator : AbstractValidator<RequestEmailChangeCommand>
{
    public RequestEmailChangeCommandValidator()
    {
        RuleFor(x => x.NewEmail)
            .NotEmpty().WithMessage("New email is required.")
            .EmailAddress().WithMessage("Enter a valid email address.")
            .MaximumLength(254);

        RuleFor(x => x.CurrentPassword).NotEmpty();
    }
}
