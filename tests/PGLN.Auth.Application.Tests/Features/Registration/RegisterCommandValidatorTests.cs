using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Features.Registration;

namespace PGLN.Auth.Application.Tests.Features.Registration;

public sealed class RegisterCommandValidatorTests
{
    [Fact]
    public async Task ValidateAsync_WithDefaultPolicyAndValidCommand_ShouldSucceed()
    {
        var validator =
            CreateValidator();

        var command =
            new RegisterCommand(
                "user@example.com",
                "LongPassword123");

        var result =
            await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyEmail_ShouldFail()
    {
        var validator =
            CreateValidator();

        var command =
            new RegisterCommand(
                string.Empty,
                "LongPassword123");

        var result =
            await validator.ValidateAsync(command);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName == nameof(RegisterCommand.Email));
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidEmail_ShouldFail()
    {
        var validator =
            CreateValidator();

        var command =
            new RegisterCommand(
                "not-an-email",
                "LongPassword123");

        var result =
            await validator.ValidateAsync(command);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName == nameof(RegisterCommand.Email));
    }

    [Fact]
    public async Task ValidateAsync_WithPasswordBelowConfiguredMinimum_ShouldFail()
    {
        var validator =
            CreateValidator(
                new PasswordPolicyOptions
                {
                    MinimumLength = 14,
                    MaximumLength = 128
                });

        var command =
            new RegisterCommand(
                "user@example.com",
                "1234567890123");

        var result =
            await validator.ValidateAsync(command);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName == nameof(RegisterCommand.Password));
    }

    [Fact]
    public async Task ValidateAsync_WithPasswordAtConfiguredMinimum_ShouldSucceed()
    {
        var validator =
            CreateValidator(
                new PasswordPolicyOptions
                {
                    MinimumLength = 14,
                    MaximumLength = 128
                });

        var command =
            new RegisterCommand(
                "user@example.com",
                "12345678901234");

        var result =
            await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WhenUppercaseRequiredAndMissing_ShouldFail()
    {
        var validator =
            CreateValidator(
                new PasswordPolicyOptions
                {
                    MinimumLength = 12,
                    MaximumLength = 128,
                    RequireUppercase = true
                });

        var command =
            new RegisterCommand(
                "user@example.com",
                "lowercase123!");

        var result =
            await validator.ValidateAsync(command);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error =>
                error.ErrorMessage.Contains(
                    "uppercase",
                    StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ValidateAsync_WhenLowercaseRequiredAndMissing_ShouldFail()
    {
        var validator =
            CreateValidator(
                new PasswordPolicyOptions
                {
                    MinimumLength = 12,
                    MaximumLength = 128,
                    RequireLowercase = true
                });

        var command =
            new RegisterCommand(
                "user@example.com",
                "UPPERCASE123!");

        var result =
            await validator.ValidateAsync(command);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error =>
                error.ErrorMessage.Contains(
                    "lowercase",
                    StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ValidateAsync_WhenDigitRequiredAndMissing_ShouldFail()
    {
        var validator =
            CreateValidator(
                new PasswordPolicyOptions
                {
                    MinimumLength = 12,
                    MaximumLength = 128,
                    RequireDigit = true
                });

        var command =
            new RegisterCommand(
                "user@example.com",
                "PasswordOnly!");

        var result =
            await validator.ValidateAsync(command);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error =>
                error.ErrorMessage.Contains(
                    "number",
                    StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ValidateAsync_WhenNonAlphanumericRequiredAndMissing_ShouldFail()
    {
        var validator =
            CreateValidator(
                new PasswordPolicyOptions
                {
                    MinimumLength = 12,
                    MaximumLength = 128,
                    RequireNonAlphanumeric = true
                });

        var command =
            new RegisterCommand(
                "user@example.com",
                "Password1234");

        var result =
            await validator.ValidateAsync(command);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error =>
                error.ErrorMessage.Contains(
                    "non-alphanumeric",
                    StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ValidateAsync_WithAllComplexityRequirementsSatisfied_ShouldSucceed()
    {
        var validator =
            CreateValidator(
                new PasswordPolicyOptions
                {
                    MinimumLength = 12,
                    MaximumLength = 128,
                    RequireUppercase = true,
                    RequireLowercase = true,
                    RequireDigit = true,
                    RequireNonAlphanumeric = true
                });

        var command =
            new RegisterCommand(
                "user@example.com",
                "VerySecure123!");

        var result =
            await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Constructor_WhenMaximumLengthIsLessThanMinimum_ShouldThrow()
    {
        var passwordPolicy =
            new PasswordPolicyOptions
            {
                MinimumLength = 20,
                MaximumLength = 10
            };

        Assert.Throws<InvalidOperationException>(
            () => new RegisterCommandValidator(passwordPolicy));
    }

    [Fact]
    public async Task ValidateAsync_ShouldNotTrimPassword()
    {
        var validator =
            CreateValidator(
                new PasswordPolicyOptions
                {
                    MinimumLength = 12,
                    MaximumLength = 128
                });

        var command =
            new RegisterCommand(
                "user@example.com",
                " passphrase ");

        var result =
            await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    private static RegisterCommandValidator CreateValidator(
        PasswordPolicyOptions? passwordPolicy = null)
    {
        return new RegisterCommandValidator(
            passwordPolicy ??
            new PasswordPolicyOptions());
    }
}
