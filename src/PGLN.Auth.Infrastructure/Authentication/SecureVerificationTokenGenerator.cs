using System.Security.Cryptography;
using PGLN.Auth.Application.Abstractions.Authentication;

namespace PGLN.Auth.Infrastructure.Authentication;

public sealed class SecureVerificationTokenGenerator
    : IVerificationTokenGenerator
{
    private const int TokenSizeInBytes = 32;

    public string Generate()
    {
        Span<byte> bytes =
            stackalloc byte[TokenSizeInBytes];

        RandomNumberGenerator.Fill(bytes);

        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(
        ReadOnlySpan<byte> bytes)
    {
        return Convert
            .ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
