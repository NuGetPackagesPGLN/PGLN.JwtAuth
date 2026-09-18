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
    public async Task Validate_WhenAuthorizationCodeIsEmpty_ShouldBeValid()
    {
        var command =
            CreateValidCommand() with
            {
                AuthorizationCode = string.Empty
            };

        var result =
            await _validator.TestValidateAsync(
                command);

        result.ShouldNotHaveValidationErrorFor(
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

    [Fact]
    public async Task Validate_WhenIpAddressIsTooLong_ShouldHaveError()
    {
        var command =
            CreateValidCommand() with
            {
                IpAddress =
                    new string('a', 65)
            };

        var result =
            await _validator.TestValidateAsync(
                command);

        result.ShouldHaveValidationErrorFor(
            value => value.IpAddress);
    }

    [Fact]
    public async Task Validate_WhenAuthorizationCodeIsPresentAndProviderErrorIsNull_ShouldBeValid()
    {
        var command =
            CreateValidCommand();

        var result =
            await _validator.TestValidateAsync(
                command);

        result.ShouldNotHaveValidationErrorFor(
            value => value.AuthorizationCode);

        result.ShouldNotHaveValidationErrorFor(
            value => value.ProviderError);
    }

    [Fact]
    public async Task Validate_WhenProviderErrorIsPresentAndAuthorizationCodeIsNull_ShouldBeValid()
    {
        var command =
            CreateValidCommand() with
            {
                AuthorizationCode = null,
                ProviderError = "access_denied"
            };

        var result =
            await _validator.TestValidateAsync(
                command);

        result.ShouldNotHaveValidationErrorFor(
            value => value.AuthorizationCode);

        result.ShouldNotHaveValidationErrorFor(
            value => value.ProviderError);
    }

    [Fact]
    public async Task Validate_WhenAuthorizationCodeAndProviderErrorAreMissing_ShouldBeValid()
    {
        var command =
            CreateValidCommand() with
            {
                AuthorizationCode = null,
                ProviderError = null
            };

        var result =
            await _validator.TestValidateAsync(
                command);

        result.ShouldNotHaveValidationErrorFor(
            value => value.AuthorizationCode);

        result.ShouldNotHaveValidationErrorFor(
            value => value.ProviderError);
    }

    [Fact]
    public async Task Validate_WhenProviderErrorIsTooLong_ShouldHaveError()
    {
        var command =
            CreateValidCommand() with
            {
                AuthorizationCode = null,
                ProviderError =
                    new string('a', 257)
            };

        var result =
            await _validator.TestValidateAsync(
                command);

        result.ShouldHaveValidationErrorFor(
            value => value.ProviderError);
    }

    private static CompleteExternalLoginCommand CreateValidCommand()
    {
        return new CompleteExternalLoginCommand(
            ExternalLoginProvider.Google,
            "authorization-code",
            "protected-state",
            "127.0.0.1",
            "test-user-agent",
            null);
    }
}
