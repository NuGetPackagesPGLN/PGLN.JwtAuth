using FluentValidation.TestHelper;
using PGLN.Auth.Application.Features.ExternalAuthentication.StartExternalLogin;
using PGLN.Auth.Domain.ExternalLogins;

namespace PGLN.Auth.Application.Tests.Features.ExternalAuthentication.StartExternalLogin;

public sealed class StartExternalLoginCommandValidatorTests
{
    private readonly StartExternalLoginCommandValidator _validator =
        new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveErrors()
    {
        var command =
            new StartExternalLoginCommand(
                ExternalLoginProvider.Google,
                "https://example.com/auth/google/callback");

        var result =
            _validator.TestValidate(
                command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenRedirectUriIsEmpty_ShouldHaveError()
    {
        var command =
            new StartExternalLoginCommand(
                ExternalLoginProvider.Google,
                string.Empty);

        var result =
            _validator.TestValidate(
                command);

        result.ShouldHaveValidationErrorFor(
            x => x.RedirectUri);
    }

    [Fact]
    public void Validate_WhenRedirectUriIsRelative_ShouldHaveError()
    {
        var command =
            new StartExternalLoginCommand(
                ExternalLoginProvider.Google,
                "/auth/google/callback");

        var result =
            _validator.TestValidate(
                command);

        result.ShouldHaveValidationErrorFor(
            x => x.RedirectUri);
    }

    [Fact]
    public void Validate_WhenRedirectUriUsesHttp_ShouldHaveError()
    {
        var command =
            new StartExternalLoginCommand(
                ExternalLoginProvider.Google,
                "http://example.com/auth/google/callback");

        var result =
            _validator.TestValidate(
                command);

        result.ShouldHaveValidationErrorFor(
            x => x.RedirectUri);
    }

    [Fact]
    public void Validate_WhenProviderIsInvalid_ShouldHaveError()
    {
        var command =
            new StartExternalLoginCommand(
                (ExternalLoginProvider)999,
                "https://example.com/auth/google/callback");

        var result =
            _validator.TestValidate(
                command);

        result.ShouldHaveValidationErrorFor(
            x => x.Provider);
    }
}
