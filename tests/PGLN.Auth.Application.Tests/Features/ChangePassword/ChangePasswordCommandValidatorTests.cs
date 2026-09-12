using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Features.ChangePassword;

namespace PGLN.Auth.Application.Tests.Features.ChangePassword;

public sealed class ChangePasswordCommandValidatorTests
{
    private static readonly PasswordPolicyOptions PasswordPolicy = new()
    {
        MinimumLength = 12,
        MaximumLength = 128,
        RequireUppercase = true,
        RequireLowercase = true,
        RequireDigit = true,
        RequireNonAlphanumeric = true
    };

    private readonly ChangePasswordCommandValidator _validator =
        new(PasswordPolicy);

    [Fact]
    public void Validate_WithValidCommand_ShouldBeValid()
    {
        ChangePasswordCommand command = new(
            Guid.NewGuid().ToString(),
            "CurrentPass1!",
            "NewPassword2!");

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldBeInvalid()
    {
        ChangePasswordCommand command = new(
            string.Empty,
            "CurrentPass1!",
            "NewPassword2!");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName ==
                nameof(ChangePasswordCommand.UserId));
    }

    [Fact]
    public void Validate_WithEmptyCurrentPassword_ShouldBeInvalid()
    {
        ChangePasswordCommand command = new(
            Guid.NewGuid().ToString(),
            string.Empty,
            "NewPassword2!");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName ==
                nameof(ChangePasswordCommand.CurrentPassword));
    }

    [Fact]
    public void Validate_WithEmptyNewPassword_ShouldBeInvalid()
    {
        ChangePasswordCommand command = new(
            Guid.NewGuid().ToString(),
            "CurrentPass1!",
            string.Empty);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName ==
                nameof(ChangePasswordCommand.NewPassword));
    }

    [Fact]
    public void Validate_WithShortNewPassword_ShouldBeInvalid()
    {
        ChangePasswordCommand command = new(
            Guid.NewGuid().ToString(),
            "CurrentPass1!",
            "Short1!");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName ==
                nameof(ChangePasswordCommand.NewPassword));
    }

    [Fact]
    public void Validate_WithoutUppercase_ShouldBeInvalid()
    {
        ChangePasswordCommand command = new(
            Guid.NewGuid().ToString(),
            "CurrentPass1!",
            "newpassword2!");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WithoutLowercase_ShouldBeInvalid()
    {
        ChangePasswordCommand command = new(
            Guid.NewGuid().ToString(),
            "CurrentPass1!",
            "NEWPASSWORD2!");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WithoutDigit_ShouldBeInvalid()
    {
        ChangePasswordCommand command = new(
            Guid.NewGuid().ToString(),
            "CurrentPass1!",
            "NewPassword!!");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WithoutNonAlphanumeric_ShouldBeInvalid()
    {
        ChangePasswordCommand command = new(
            Guid.NewGuid().ToString(),
            "CurrentPass1!",
            "NewPassword22");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenNewPasswordMatchesCurrentPassword_ShouldBeInvalid()
    {
        ChangePasswordCommand command = new(
            Guid.NewGuid().ToString(),
            "CurrentPass1!",
            "CurrentPass1!");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error =>
                error.ErrorMessage ==
                "New password must be different from the current password.");
    }
}
