using FluentValidation;
using PGLN.Auth.Application.Abstractions.Authentication;

namespace PGLN.Auth.Application.Features.ResetPassword;

public sealed class ResetPasswordCommandValidator
    : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator(
        PasswordPolicyOptions passwordPolicy)
    {
        ArgumentNullException.ThrowIfNull(
            passwordPolicy);

        passwordPolicy.Validate();

        RuleFor(command => command.Email)
            .NotEmpty()
            .WithMessage(
                "Email is required.")
            .MaximumLength(320)
            .WithMessage(
                "Email must not exceed 320 characters.")
            .EmailAddress()
            .WithMessage(
                "Email must be a valid email address.");

        RuleFor(command => command.ResetToken)
            .NotEmpty()
            .WithMessage(
                "Password reset token is required.")
            .MaximumLength(2048)
            .WithMessage(
                "Password reset token must not exceed 2048 characters.");

        RuleFor(command => command.NewPassword)
            .NotEmpty()
            .WithMessage(
                "Password is required.")
            .MinimumLength(
                passwordPolicy.MinimumLength)
            .WithMessage(
                $"Password must be at least {passwordPolicy.MinimumLength} characters long.")
            .MaximumLength(
                passwordPolicy.MaximumLength)
            .WithMessage(
                $"Password must not exceed {passwordPolicy.MaximumLength} characters.");

        if (passwordPolicy.RequireUppercase)
        {
            RuleFor(command => command.NewPassword)
                .Must(ContainsUppercase)
                .WithMessage(
                    "Password must contain at least one uppercase letter.");
        }

        if (passwordPolicy.RequireLowercase)
        {
            RuleFor(command => command.NewPassword)
                .Must(ContainsLowercase)
                .WithMessage(
                    "Password must contain at least one lowercase letter.");
        }

        if (passwordPolicy.RequireDigit)
        {
            RuleFor(command => command.NewPassword)
                .Must(ContainsDigit)
                .WithMessage(
                    "Password must contain at least one number.");
        }

        if (passwordPolicy.RequireNonAlphanumeric)
        {
            RuleFor(command => command.NewPassword)
                .Must(ContainsNonAlphanumeric)
                .WithMessage(
                    "Password must contain at least one non-alphanumeric character.");
        }
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
