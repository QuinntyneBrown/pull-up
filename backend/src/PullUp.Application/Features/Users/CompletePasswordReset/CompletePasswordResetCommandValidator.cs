using System.Text.RegularExpressions;
using FluentValidation;

namespace PullUp.Application.Features.Users.CompletePasswordReset;

public sealed class CompletePasswordResetCommandValidator : AbstractValidator<CompletePasswordResetCommand>
{
    private static readonly Regex HasDigit = new("[0-9]", RegexOptions.Compiled);
    private static readonly Regex HasSymbol = new(@"[^a-zA-Z0-9]", RegexOptions.Compiled);

    public CompletePasswordResetCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().WithMessage("Token is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("At least 8 characters, one number, one symbol.")
            .Must(p => HasDigit.IsMatch(p)).WithMessage("At least 8 characters, one number, one symbol.")
            .Must(p => HasSymbol.IsMatch(p)).WithMessage("At least 8 characters, one number, one symbol.");
    }
}
