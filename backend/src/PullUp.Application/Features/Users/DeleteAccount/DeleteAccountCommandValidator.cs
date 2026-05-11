using FluentValidation;

namespace PullUp.Application.Features.Users.DeleteAccount;

public sealed class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
{
    public DeleteAccountCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
    }
}
