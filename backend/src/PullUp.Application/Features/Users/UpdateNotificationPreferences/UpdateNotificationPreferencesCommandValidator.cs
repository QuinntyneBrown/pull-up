using FluentValidation;

namespace PullUp.Application.Features.Users.UpdateNotificationPreferences;

public sealed class UpdateNotificationPreferencesCommandValidator
    : AbstractValidator<UpdateNotificationPreferencesCommand>
{
    public UpdateNotificationPreferencesCommandValidator()
    {
        // Bools are always valid; no rules. Validator exists for symmetry with the rest
        // of the command surface and so future fields (e.g. quiet hours) have a home.
    }
}
