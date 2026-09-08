using PGLN.Auth.Application.Features.TokenRefresh;

namespace PGLN.Auth.Application.Tests.Features.TokenRefresh;

public sealed class TokenRefreshCommandValidatorTests
{
    private readonly TokenRefreshCommandValidator _validator =
        new();

    [Fact]
    public void Validate_WithToken_ShouldSucceed()
    {
        var result =
            _validator.Validate(
                new TokenRefreshCommand(
                    "refresh-token"));

        Assert.True(
            result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyToken_ShouldFail()
    {
        var result =
            _validator.Validate(
                new TokenRefreshCommand(
                    string.Empty));

        Assert.False(
            result.IsValid);
    }
}
