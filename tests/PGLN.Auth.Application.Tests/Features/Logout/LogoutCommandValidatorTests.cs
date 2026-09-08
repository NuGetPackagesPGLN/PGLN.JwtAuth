using PGLN.Auth.Application.Features.Logout;

namespace PGLN.Auth.Application.Tests.Features.Logout;

public sealed class LogoutCommandValidatorTests
{
    private readonly LogoutCommandValidator _validator =
        new();

    [Fact]
    public void Validate_WithRefreshToken_ShouldSucceed()
    {
        var result =
            _validator.Validate(
                new LogoutCommand(
                    "refresh-token"));

        Assert.True(
            result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyRefreshToken_ShouldFail()
    {
        var result =
            _validator.Validate(
                new LogoutCommand(
                    string.Empty));

        Assert.False(
            result.IsValid);
    }
}
