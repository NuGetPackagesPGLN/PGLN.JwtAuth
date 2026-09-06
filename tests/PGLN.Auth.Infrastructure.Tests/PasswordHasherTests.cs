using PGLN.Auth.Infrastructure.Passwords;

namespace PGLN.Auth.Infrastructure.Tests.Passwords;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Hash_WithValidPassword_ShouldReturnHash()
    {
        var hasher =
            new PasswordHasher();

        var hash =
            hasher.Hash(
                "VerySecurePassword123!");

        Assert.False(
            string.IsNullOrWhiteSpace(hash));

        Assert.NotEqual(
            "VerySecurePassword123!",
            hash);
    }

    [Fact]
    public void Hash_SamePasswordTwice_ShouldProduceDifferentHashes()
    {
        var hasher =
            new PasswordHasher();

        var firstHash =
            hasher.Hash(
                "VerySecurePassword123!");

        var secondHash =
            hasher.Hash(
                "VerySecurePassword123!");

        Assert.NotEqual(
            firstHash,
            secondHash);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ShouldReturnTrue()
    {
        var hasher =
            new PasswordHasher();

        const string password =
            "VerySecurePassword123!";

        var hash =
            hasher.Hash(password);

        var verified =
            hasher.Verify(
                password,
                hash);

        Assert.True(verified);
    }

    [Fact]
    public void Verify_WithIncorrectPassword_ShouldReturnFalse()
    {
        var hasher =
            new PasswordHasher();

        var hash =
            hasher.Hash(
                "CorrectPassword123!");

        var verified =
            hasher.Verify(
                "WrongPassword123!",
                hash);

        Assert.False(verified);
    }

    [Fact]
    public void Hash_WithEmptyPassword_ShouldThrow()
    {
        var hasher =
            new PasswordHasher();

        Assert.Throws<ArgumentException>(
            () => hasher.Hash(string.Empty));
    }

    [Fact]
    public void Verify_WithEmptyHash_ShouldThrow()
    {
        var hasher =
            new PasswordHasher();

        Assert.Throws<ArgumentException>(
            () =>
                hasher.Verify(
                    "ValidPassword123!",
                    string.Empty));
    }
}
