using PGLN.Auth.Application.Features.Login;

namespace PGLN.Auth.Application.Tests.Features.Login;

public sealed class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator =
        new();

    [Fact]
    public void Validate_WithValidCredentials_ShouldSucceed()
    {
        var result =
            _validator.Validate(
                new LoginCommand(
                    "user@example.com",
                    "anything"));

        Assert.True(
            result.IsValid);
    }

    [Fact]
    public void Validate_WithInvalidEmail_ShouldFail()
    {
        var result =
            _validator.Validate(
                new LoginCommand(
                    "not-an-email",
                    "anything"));

        Assert.False(
            result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyPassword_ShouldFail()
    {
        var result =
            _validator.Validate(
                new LoginCommand(
                    "user@example.com",
                    string.Empty));

        Assert.False(
            result.IsValid);
    }
}
