using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using PGLN.Auth.Application.Abstractions.Authentication;

namespace PGLN.Auth.Infrastructure.Authentication;

public sealed class SecureRefreshTokenGenerator
    : IRefreshTokenGenerator
{
    private const int TokenSizeInBytes =
        64;

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
