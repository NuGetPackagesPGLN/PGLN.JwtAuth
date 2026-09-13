using PGLN.Auth.Infrastructure.Authentication;

namespace PGLN.Auth.Infrastructure.Tests.Authentication;

public sealed class HmacStepUpCodeProtectorTests
{
    private const string Secret =
        "this-is-a-test-secret-that-is-at-least-32-characters";

    [Fact]
    public void Protect_ThenVerify_WithCorrectCode_ShouldReturnTrue()
    {
        var protector =
            new HmacStepUpCodeProtector(
                Secret);

        const string code =
            "123456";

        var protectedCode =
            protector.Protect(
                code);

        var result =
            protector.Verify(
                code,
                protectedCode);

        Assert.True(
            result);
    }

    [Fact]
    public void Verify_WithIncorrectCode_ShouldReturnFalse()
    {
        var protector =
            new HmacStepUpCodeProtector(
                Secret);

        var protectedCode =
            protector.Protect(
                "123456");

        var result =
            protector.Verify(
                "654321",
                protectedCode);

        Assert.False(
            result);
    }

    [Fact]
    public void Verify_WithMalformedProtectedCode_ShouldReturnFalse()
    {
        var protector =
            new HmacStepUpCodeProtector(
                Secret);

        var result =
            protector.Verify(
                "123456",
                "not-valid-hex");

        Assert.False(
            result);
    }

    [Fact]
    public void Verify_WithEmptyProtectedCode_ShouldReturnFalse()
    {
        var protector =
            new HmacStepUpCodeProtector(
                Secret);

        var result =
            protector.Verify(
                "123456",
                string.Empty);

        Assert.False(
            result);
    }

    [Fact]
    public void Protect_WithEmptyCode_ShouldThrow()
    {
        var protector =
            new HmacStepUpCodeProtector(
                Secret);

        Assert.Throws<ArgumentException>(
            () =>
                protector.Protect(
                    string.Empty));
    }
}
