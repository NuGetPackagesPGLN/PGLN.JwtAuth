using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Infrastructure.Authentication;

public sealed class JwtAccessTokenGenerator
    : IAccessTokenGenerator
{
    private readonly JwtOptions _options;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwtAccessTokenGenerator(
        JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(
            options);

        options.Validate();

        _options =
            options;

        _tokenHandler =
            new JwtSecurityTokenHandler
            {
                MapInboundClaims = false
            };
    }

    public AccessTokenResult Generate(
        User user,
        DateTimeOffset issuedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(
            user);

        var expiresAtUtc =
            issuedAtUtc.Add(
                _options.AccessTokenLifetime);

        var signingKey =
            new SymmetricSecurityKey(
                _options.GetSigningKeyBytes());

        var credentials =
            new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256);

        var claims =
            new[]
            {
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    user.Id.Value.ToString()),

                new Claim(
                    JwtRegisteredClaimNames.Email,
                    user.Email.Value),

                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString()),

                new Claim(
                    JwtRegisteredClaimNames.Iat,
                    issuedAtUtc
                        .ToUnixTimeSeconds()
                        .ToString(),
                    ClaimValueTypes.Integer64)
            };

        var token =
            new JwtSecurityToken(
                issuer:
                    _options.Issuer,
                audience:
                    _options.Audience,
                claims:
                    claims,
                notBefore:
                    issuedAtUtc.UtcDateTime,
                expires:
                    expiresAtUtc.UtcDateTime,
                signingCredentials:
                    credentials);

        var serializedToken =
            _tokenHandler.WriteToken(
                token);

        return new AccessTokenResult(
            serializedToken,
            expiresAtUtc);
    }
}
