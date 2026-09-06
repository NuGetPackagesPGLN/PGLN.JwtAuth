using PGLN.Auth.Infrastructure.Authentication;

namespace PGLN.Auth.Infrastructure.Tests.Authentication;

public sealed class Sha256TokenHasherTests
{
    [Fact]
    public void Hash_WithValidToken_ShouldReturnHash()
    {
        var hasher =
            new Sha256TokenHasher();

        var hash =
            hasher.Hash(
                "verification-token");

        Assert.False(
            string.IsNullOrWhiteSpace(hash));

        Assert.NotEqual(
            "verification-token",
            hash);
    }

    [Fact]
    public void Hash_SameToken_ShouldProduceSameHash()
    {
        var hasher =
            new Sha256TokenHasher();

        var first =
            hasher.Hash(
                "verification-token");

        var second =
            hasher.Hash(
                "verification-token");

        Assert.Equal(
            first,
            second);
    }

    [Fact]
    public void Hash_DifferentTokens_ShouldProduceDifferentHashes()
    {
        var hasher =
            new Sha256TokenHasher();

        var first =
            hasher.Hash(
                "verification-token-one");

        var second =
            hasher.Hash(
                "verification-token-two");

        Assert.NotEqual(
            first,
            second);
    }

    [Fact]
    public void Hash_ShouldReturnSha256HexLength()
    {
        var hasher =
            new Sha256TokenHasher();

        var hash =
            hasher.Hash(
                "verification-token");

        Assert.Equal(
            64,
            hash.Length);
    }

    [Fact]
    public void Hash_WithEmptyToken_ShouldThrow()
    {
        var hasher =
            new Sha256TokenHasher();

        Assert.Throws<ArgumentException>(
            () =>
                hasher.Hash(
                    string.Empty));
    }
}
