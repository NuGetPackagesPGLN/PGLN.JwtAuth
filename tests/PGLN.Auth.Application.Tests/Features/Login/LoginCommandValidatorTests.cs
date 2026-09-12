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
                    "anything",
                    "device-hash-001",
                    "Test Device",
                    "127.0.0.1",
                    "TestAgent/1.0"));

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
                    "anything",
                    "device-hash-001",
                    "Test Device",
                    "127.0.0.1",
                    "TestAgent/1.0"));

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
                    string.Empty,
                    "device-hash-001",
                    "Test Device",
                    "127.0.0.1",
                    "TestAgent/1.0"));

        Assert.False(
            result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyDeviceIdHash_ShouldFail()
    {
        var result =
            _validator.Validate(
                new LoginCommand(
                    "user@example.com",
                    "anything",
                    string.Empty,
                    "Test Device",
                    "127.0.0.1",
                    "TestAgent/1.0"));

        Assert.False(
            result.IsValid);
    }
}
