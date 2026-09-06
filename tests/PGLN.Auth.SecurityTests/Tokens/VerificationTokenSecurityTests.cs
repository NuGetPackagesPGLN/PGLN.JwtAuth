using PGLN.Auth.Infrastructure.Authentication;

namespace PGLN.Auth.SecurityTests.Tokens;

public sealed class VerificationTokenSecurityTests
{
    [Fact]
    public void GeneratedTokens_ShouldHaveAtLeast256BitsOfRandomness()
    {
        var generator =
            new SecureVerificationTokenGenerator();

        var token =
            generator.Generate();

        Assert.True(
            token.Length >= 43);
    }

    [Fact]
    public void GeneratedTokens_ShouldNotContainPredictableSequence()
    {
        var generator =
            new SecureVerificationTokenGenerator();

        var first =
            generator.Generate();

        var second =
            generator.Generate();

        Assert.NotEqual(
            first,
            second);
    }

    [Fact]
    public void TokenHash_ShouldNotContainRawToken()
    {
        var generator =
            new SecureVerificationTokenGenerator();

        var hasher =
            new Sha256TokenHasher();

        var token =
            generator.Generate();

        var hash =
            hasher.Hash(token);

        Assert.DoesNotContain(
            token,
            hash,
            StringComparison.Ordinal);
    }
}
