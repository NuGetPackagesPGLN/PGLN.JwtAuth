using PGLN.Auth.Application.Features.LogoutAll;

namespace PGLN.Auth.Application.Tests.Features.LogoutAll;

public sealed class LogoutAllCommandValidatorTests
{
    private readonly LogoutAllCommandValidator _validator =
        new();

    [Fact]
    public void Validate_WithRefreshToken_ShouldSucceed()
    {
        var result =
            _validator.Validate(
                new LogoutAllCommand(
                    "refresh-token"));

        Assert.True(
            result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyRefreshToken_ShouldFail()
    {
        var result =
            _validator.Validate(
                new LogoutAllCommand(
                    string.Empty));

        Assert.False(
            result.IsValid);
    }
}
