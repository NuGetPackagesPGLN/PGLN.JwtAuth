using System.Text;

namespace PGLN.Auth.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public string Issuer { get; init; } =
        string.Empty;

    public string Audience { get; init; } =
        string.Empty;

    /// <summary>
    /// Base64-encoded symmetric signing key.
    ///
    /// Production applications should obtain this value from a
    /// secret manager, environment variable, or equivalent secure
    /// configuration source. It must not be committed to source control.
    /// </summary>
    public string SigningKey { get; init; } =
        string.Empty;

    public TimeSpan AccessTokenLifetime { get; init; } =
        TimeSpan.FromMinutes(15);

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            Issuer);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            Audience);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            SigningKey);

        if (AccessTokenLifetime <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "JWT access token lifetime must be greater than zero.");
        }

        byte[] signingKeyBytes;

        try
        {
            signingKeyBytes =
                Convert.FromBase64String(
                    SigningKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                "JWT signing key must be valid Base64.",
                exception);
        }

        if (signingKeyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must contain at least 256 bits of key material.");
        }
    }

    internal byte[] GetSigningKeyBytes()
    {
        Validate();

        return Convert.FromBase64String(
            SigningKey);
    }
}
