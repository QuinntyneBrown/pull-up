using System.Text.RegularExpressions;
using FluentValidation;

namespace PullUp.Application.Features.Users.RegisterUser;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    private static readonly Regex HasDigit = new("[0-9]", RegexOptions.Compiled);
    private static readonly Regex HasSymbol = new(@"[^a-zA-Z0-9]", RegexOptions.Compiled);

    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Enter a valid email address.")
            .MaximumLength(254).WithMessage("Email is too long.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("At least 8 characters, one number, one symbol.")
            .Must(p => HasDigit.IsMatch(p)).WithMessage("At least 8 characters, one number, one symbol.")
            .Must(p => HasSymbol.IsMatch(p)).WithMessage("At least 8 characters, one number, one symbol.");
    }
}
