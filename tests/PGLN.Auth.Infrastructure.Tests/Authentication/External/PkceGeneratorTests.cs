using System.Security.Cryptography;
using System.Text;
using PGLN.Auth.Infrastructure.Authentication.External;

namespace PGLN.Auth.Infrastructure.Tests.Authentication.External;

public sealed class PkceGeneratorTests
{
    [Fact]
    public void Generate_ShouldCreateValidPkcePair()
    {
        var generator =
            new PkceGenerator();

        var result =
            generator.Generate();

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.CodeVerifier));

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.CodeChallenge));

        var expectedChallenge =
            Base64UrlEncode(
                SHA256.HashData(
                    Encoding.ASCII.GetBytes(
                        result.CodeVerifier)));

        Assert.Equal(
            expectedChallenge,
            result.CodeChallenge);
    }

    [Fact]
    public void Generate_ShouldCreateDifferentValuesEachTime()
    {
        var generator =
            new PkceGenerator();

        var first =
            generator.Generate();

        var second =
            generator.Generate();

        Assert.NotEqual(
            first.CodeVerifier,
            second.CodeVerifier);

        Assert.NotEqual(
            first.CodeChallenge,
            second.CodeChallenge);
    }

    private static string Base64UrlEncode(
        byte[] bytes)
    {
        return Convert
            .ToBase64String(
                bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
