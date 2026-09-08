using Microsoft.IdentityModel.Tokens;
using PGLN.Auth.Infrastructure.Authentication;

namespace PGLN.Auth.Infrastructure.Tests.Authentication;

public sealed class SecureRefreshTokenGeneratorTests
{
    [Fact]
    public void Generate_ShouldReturnToken()
    {
        var generator =
            new SecureRefreshTokenGenerator();

        var token =
            generator.Generate();

        Assert.False(
            string.IsNullOrWhiteSpace(
                token));
    }

    [Fact]
    public void Generate_ShouldContain512BitsOfRandomData()
    {
        var generator =
            new SecureRefreshTokenGenerator();

        var token =
            generator.Generate();

        var bytes =
            Base64UrlEncoder.DecodeBytes(
                token);

        Assert.Equal(
            64,
            bytes.Length);
    }

    [Fact]
    public void Generate_ShouldUseBase64UrlEncoding()
    {
        var generator =
            new SecureRefreshTokenGenerator();

        var token =
            generator.Generate();

        Assert.DoesNotContain(
            "+",
            token);

        Assert.DoesNotContain(
            "/",
            token);

        Assert.DoesNotContain(
            "=",
            token);
    }

    [Fact]
    public void Generate_ShouldProduceDifferentTokens()
    {
        var generator =
            new SecureRefreshTokenGenerator();

        var first =
            generator.Generate();

        var second =
            generator.Generate();

        Assert.NotEqual(
            first,
            second);
    }

    [Fact]
    public void Generate_MultipleTimes_ShouldProduceUniqueTokens()
    {
        var generator =
            new SecureRefreshTokenGenerator();

        var tokens =
            Enumerable.Range(
                    0,
                    100)
                .Select(
                    _ =>
                        generator.Generate())
                .ToHashSet();

        Assert.Equal(
            100,
            tokens.Count);
    }
}
