using PGLN.Auth.Infrastructure.Authentication;

namespace PGLN.Auth.Infrastructure.Tests.Authentication;

public sealed class SecureVerificationTokenGeneratorTests
{
    [Fact]
    public void Generate_ShouldReturnToken()
    {
        var generator =
            new SecureVerificationTokenGenerator();

        var token =
            generator.Generate();

        Assert.False(
            string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void Generate_ShouldReturnUrlSafeToken()
    {
        var generator =
            new SecureVerificationTokenGenerator();

        for (var index = 0; index < 100; index++)
        {
            var token =
                generator.Generate();

            Assert.DoesNotContain("+", token);
            Assert.DoesNotContain("/", token);
            Assert.DoesNotContain("=", token);
        }
    }

    [Fact]
    public void Generate_RepeatedCalls_ShouldProduceDifferentTokens()
    {
        var generator =
            new SecureVerificationTokenGenerator();

        var tokens =
            Enumerable
                .Range(0, 100)
                .Select(_ => generator.Generate())
                .ToArray();

        Assert.Equal(
            tokens.Length,
            tokens.Distinct().Count());
    }

    [Fact]
    public void Generate_ShouldHaveExpectedEncodedLength()
    {
        var generator =
            new SecureVerificationTokenGenerator();

        var token =
            generator.Generate();

        Assert.Equal(
            43,
            token.Length);
    }
}
