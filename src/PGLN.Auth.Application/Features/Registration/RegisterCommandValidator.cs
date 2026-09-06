using FluentValidation;
using PGLN.Auth.Application.Abstractions.Authentication;

namespace PGLN.Auth.Application.Features.Registration;

public sealed class RegisterCommandValidator
    : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator(
        PasswordPolicyOptions passwordPolicy)
    {
        ArgumentNullException.ThrowIfNull(passwordPolicy);

        passwordPolicy.Validate();

        RuleFor(command => command.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .MaximumLength(320)
            .WithMessage("Email must not exceed 320 characters.")
            .EmailAddress()
            .WithMessage("Email must be a valid email address.");

        RuleFor(command => command.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(passwordPolicy.MinimumLength)
            .WithMessage(
                $"Password must be at least {passwordPolicy.MinimumLength} characters long.")
            .MaximumLength(passwordPolicy.MaximumLength)
            .WithMessage(
                $"Password must not exceed {passwordPolicy.MaximumLength} characters.");

        if (passwordPolicy.RequireUppercase)
        {
            RuleFor(command => command.Password)
                .Must(ContainsUppercase)
                .WithMessage(
                    "Password must contain at least one uppercase letter.");
        }

        if (passwordPolicy.RequireLowercase)
        {
            RuleFor(command => command.Password)
                .Must(ContainsLowercase)
                .WithMessage(
                    "Password must contain at least one lowercase letter.");
        }

        if (passwordPolicy.RequireDigit)
        {
            RuleFor(command => command.Password)
                .Must(ContainsDigit)
                .WithMessage(
                    "Password must contain at least one number.");
        }

        if (passwordPolicy.RequireNonAlphanumeric)
        {
            RuleFor(command => command.Password)
                .Must(ContainsNonAlphanumeric)
                .WithMessage(
                    "Password must contain at least one non-alphanumeric character.");
        }
    }

    private static bool ContainsUppercase(string password)
    {
        return password.Any(char.IsUpper);
    }

    private static bool ContainsLowercase(string password)
    {
        return password.Any(char.IsLower);
    }

    private static bool ContainsDigit(string password)
    {
        return password.Any(char.IsDigit);
    }

    private static bool ContainsNonAlphanumeric(string password)
    {
        return password.Any(
            character => !char.IsLetterOrDigit(character));
    }
}
