using System.Security.Cryptography;
using System.Text;
using PGLN.Auth.Application.Abstractions.ExternalAuthentication;

namespace PGLN.Auth.Infrastructure.Authentication.External;

public sealed class PkceGenerator
    : IPkceGenerator
{
    public PkcePair Generate()
    {
        var verifierBytes =
            RandomNumberGenerator.GetBytes(
                32);

        var codeVerifier =
            Base64UrlEncode(
                verifierBytes);

        var challengeBytes =
            SHA256.HashData(
                Encoding.ASCII.GetBytes(
                    codeVerifier));

        var codeChallenge =
            Base64UrlEncode(
                challengeBytes);

        return new PkcePair(
            codeVerifier,
            codeChallenge);
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
