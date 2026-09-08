using System.Security.Cryptography;
using PGLN.Auth.Infrastructure.Authentication;

namespace PGLN.Auth.Infrastructure.Tests.Authentication;

public sealed class JwtOptionsTests
{
    [Fact]
    public void Validate_WithValidOptions_ShouldNotThrow()
    {
        var options =
            CreateValidOptions();

        options.Validate();
    }

    [Fact]
    public void Validate_WithShortSigningKey_ShouldThrow()
    {
        var options =
            new JwtOptions
            {
                Issuer =
                    "PGLN.Auth.Tests",

                Audience =
                    "PGLN.Auth.Tests.Client",

                SigningKey =
                    Convert.ToBase64String(
                        RandomNumberGenerator.GetBytes(16))
            };

        Assert.Throws<
            InvalidOperationException>(
            options.Validate);
    }

    [Fact]
    public void Validate_WithInvalidBase64SigningKey_ShouldThrow()
    {
        var options =
            new JwtOptions
            {
                Issuer =
                    "PGLN.Auth.Tests",

                Audience =
                    "PGLN.Auth.Tests.Client",

                SigningKey =
                    "definitely-not-base64"
            };

        Assert.Throws<
            InvalidOperationException>(
            options.Validate);
    }

    [Fact]
    public void Validate_WithNonPositiveLifetime_ShouldThrow()
    {
        var options =
            new JwtOptions
            {
                Issuer =
                    "PGLN.Auth.Tests",

                Audience =
                    "PGLN.Auth.Tests.Client",

                SigningKey =
                    Convert.ToBase64String(
                        RandomNumberGenerator.GetBytes(32)),

                AccessTokenLifetime =
                    TimeSpan.Zero
            };

        Assert.Throws<
            InvalidOperationException>(
            options.Validate);
    }

    private static JwtOptions CreateValidOptions()
    {
        return new JwtOptions
        {
            Issuer =
                "PGLN.Auth.Tests",

            Audience =
                "PGLN.Auth.Tests.Client",

            SigningKey =
                Convert.ToBase64String(
                    RandomNumberGenerator.GetBytes(32)),

            AccessTokenLifetime =
                TimeSpan.FromMinutes(15)
        };
    }
}
