using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using PGLN.Auth.Application.Abstractions.Security;

namespace PGLN.Auth.Infrastructure.Authentication;

public sealed class SecurePasswordResetTokenGenerator
    : IPasswordResetTokenGenerator
{
    private const int TokenSizeInBytes =
        32;

    public string Generate()
    {
        Span<byte> bytes =
            stackalloc byte[TokenSizeInBytes];

        RandomNumberGenerator.Fill(
            bytes);

        return Base64UrlEncoder.Encode(
            bytes.ToArray());
    }
}
