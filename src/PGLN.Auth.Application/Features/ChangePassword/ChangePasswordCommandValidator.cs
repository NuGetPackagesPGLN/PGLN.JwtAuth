using FluentValidation;
using PGLN.Auth.Application.Abstractions.Authentication;

namespace PGLN.Auth.Application.Features.ChangePassword;

public sealed class ChangePasswordCommandValidator
    : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator(
        PasswordPolicyOptions passwordPolicy)
    {
        ArgumentNullException.ThrowIfNull(
            passwordPolicy);

        passwordPolicy.Validate();

        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage(
                "User id is required.");

        RuleFor(command => command.CurrentPassword)
            .NotEmpty()
            .WithMessage(
                "Current password is required.")
            .MaximumLength(
                passwordPolicy.MaximumLength)
            .WithMessage(
                $"Current password must not exceed {passwordPolicy.MaximumLength} characters.");

        RuleFor(command => command.NewPassword)
            .NotEmpty()
            .WithMessage(
                "New password is required.")
            .MinimumLength(
                passwordPolicy.MinimumLength)
            .WithMessage(
                $"New password must be at least {passwordPolicy.MinimumLength} characters long.")
            .MaximumLength(
                passwordPolicy.MaximumLength)
            .WithMessage(
                $"New password must not exceed {passwordPolicy.MaximumLength} characters.");

        if (passwordPolicy.RequireUppercase)
        {
            RuleFor(command => command.NewPassword)
                .Must(ContainsUppercase)
                .WithMessage(
                    "New password must contain at least one uppercase letter.");
        }

        if (passwordPolicy.RequireLowercase)
        {
            RuleFor(command => command.NewPassword)
                .Must(ContainsLowercase)
                .WithMessage(
                    "New password must contain at least one lowercase letter.");
        }

        if (passwordPolicy.RequireDigit)
        {
            RuleFor(command => command.NewPassword)
                .Must(ContainsDigit)
                .WithMessage(
                    "New password must contain at least one number.");
        }

        if (passwordPolicy.RequireNonAlphanumeric)
        {
            RuleFor(command => command.NewPassword)
                .Must(ContainsNonAlphanumeric)
                .WithMessage(
                    "New password must contain at least one non-alphanumeric character.");
        }

        RuleFor(command => command.NewPassword)
            .NotEqual(command => command.CurrentPassword)
            .WithMessage(
                "New password must be different from the current password.");
    }

    private static bool ContainsUppercase(
        string password)
    {
        return password.Any(
            char.IsUpper);
    }

    private static bool ContainsLowercase(
        string password)
    {
        return password.Any(
            char.IsLower);
    }

    private static bool ContainsDigit(
        string password)
    {
        return password.Any(
            char.IsDigit);
    }

    private static bool ContainsNonAlphanumeric(
        string password)
    {
        return password.Any(
            character =>
                !char.IsLetterOrDigit(
                    character));
    }
}
