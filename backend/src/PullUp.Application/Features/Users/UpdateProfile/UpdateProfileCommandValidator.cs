using FluentValidation;

namespace PullUp.Application.Features.Users.UpdateProfile;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MaximumLength(40).WithMessage("Display name cannot exceed 40 characters.");
    }
}
