using FluentValidation.TestHelper;
using PGLN.Auth.Application.Features.ExternalAuthentication.CompleteExternalLogin;
using PGLN.Auth.Domain.ExternalLogins;

namespace PGLN.Auth.Application.Tests.Features.ExternalAuthentication.CompleteExternalLogin;

public sealed class CompleteExternalLoginCommandValidatorTests
{
    private readonly CompleteExternalLoginCommandValidator _validator =
        new();

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveErrors()
    {
        var command =
            CreateValidCommand();

        var result =
            await _validator.TestValidateAsync(
                command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenAuthorizationCodeIsEmpty_ShouldHaveError()
    {
        var command =
            CreateValidCommand() with
            {
                AuthorizationCode = string.Empty
            };

        var result =
            await _validator.TestValidateAsync(
                command);

        result.ShouldHaveValidationErrorFor(
            value => value.AuthorizationCode);
    }

    [Fact]
    public async Task Validate_WhenStateIsEmpty_ShouldHaveError()
    {
        var command =
            CreateValidCommand() with
            {
                State = string.Empty
            };

        var result =
            await _validator.TestValidateAsync(
                command);

        result.ShouldHaveValidationErrorFor(
            value => value.State);
    }

    [Fact]
    public async Task Validate_WhenRedirectUriIsEmpty_ShouldHaveError()
    {
        var command =
            CreateValidCommand() with
            {
                RedirectUri = string.Empty
            };

        var result =
            await _validator.TestValidateAsync(
                command);

        result.ShouldHaveValidationErrorFor(
            value => value.RedirectUri);
    }

    [Fact]
    public async Task Validate_WhenProviderIsInvalid_ShouldHaveError()
    {
        var command =
            CreateValidCommand() with
            {
                Provider =
                    (ExternalLoginProvider)999
            };

        var result =
            await _validator.TestValidateAsync(
                command);

        result.ShouldHaveValidationErrorFor(
            value => value.Provider);
    }

    [Fact]
    public async Task Validate_WhenAuthorizationCodeIsTooLong_ShouldHaveError()
    {
        var command =
            CreateValidCommand() with
            {
                AuthorizationCode =
                    new string('a', 4097)
            };

        var result =
            await _validator.TestValidateAsync(
                command);

        result.ShouldHaveValidationErrorFor(
            value => value.AuthorizationCode);
    }

    [Fact]
    public async Task Validate_WhenRedirectUriIsTooLong_ShouldHaveError()
    {
        var command =
            CreateValidCommand() with
            {
                RedirectUri =
                    new string('a', 2049)
            };

        var result =
            await _validator.TestValidateAsync(
                command);

        result.ShouldHaveValidationErrorFor(
            value => value.RedirectUri);
    }

    [Fact]
    public async Task Validate_WhenDeviceIdHashIsTooLong_ShouldHaveError()
    {
        var command =
            CreateValidCommand() with
            {
                DeviceIdHash =
                    new string('a', 257)
            };

        var result =
            await _validator.TestValidateAsync(
                command);

        result.ShouldHaveValidationErrorFor(
            value => value.DeviceIdHash);
    }

    [Fact]
    public async Task Validate_WhenUserAgentIsTooLong_ShouldHaveError()
    {
        var command =
            CreateValidCommand() with
            {
                UserAgent =
                    new string('a', 1025)
            };

        var result =
            await _validator.TestValidateAsync(
                command);

        result.ShouldHaveValidationErrorFor(
            value => value.UserAgent);
    }

    private static CompleteExternalLoginCommand CreateValidCommand()
    {
        return new CompleteExternalLoginCommand(
            ExternalLoginProvider.Google,
            "authorization-code",
            "protected-state",
            "https://localhost/signin-google",
            "device-hash",
            "Chrome on Windows",
            "127.0.0.1",
            "test-user-agent");
    }

    [Fact]
    public void Validate_WhenDeviceIdHashIsEmpty_ShouldFail()
    {
        var validator =
            new CompleteExternalLoginCommandValidator();

        var command =
            new CompleteExternalLoginCommand(
                ExternalLoginProvider.Google,
                "authorization-code",
                "protected-state",
                "https://localhost/signin-google",
                string.Empty,
                "Chrome on Windows",
                "127.0.0.1",
                "Chrome/1.0");

        var result =
            validator.Validate(command);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName ==
                nameof(CompleteExternalLoginCommand.DeviceIdHash));
    }
}



