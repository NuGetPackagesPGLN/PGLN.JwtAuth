using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.Infrastructure.Authentication;

namespace PGLN.Auth.Infrastructure.Tests.Authentication;

public sealed class JwtAccessTokenGeneratorTests
{
    [Fact]
    public void Generate_ShouldReturnSignedJwt()
    {
        var signingKey =
            RandomNumberGenerator.GetBytes(
                32);

        var options =
            CreateOptions(
                signingKey);

        var generator =
            new JwtAccessTokenGenerator(
                options);

        var user =
            CreateUser();

        var sessionId =
            AuthSessionId.New();

        var issuedAt =
            DateTimeOffset.UtcNow;

        var result =
            generator.Generate(
                user,
                sessionId,
                issuedAt);

        var handler =
            new JwtSecurityTokenHandler
            {
                MapInboundClaims = false
            };

        var principal =
            handler.ValidateToken(
                result.Token,
                new TokenValidationParameters
                {
                    ValidateIssuer =
                        true,

                    ValidIssuer =
                        options.Issuer,

                    ValidateAudience =
                        true,

                    ValidAudience =
                        options.Audience,

                    ValidateIssuerSigningKey =
                        true,

                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            signingKey),

                    ValidateLifetime =
                        true,

                    ClockSkew =
                        TimeSpan.Zero
                },
                out var validatedToken);

        Assert.NotNull(
            principal);

        var jwt =
            Assert.IsType<JwtSecurityToken>(
                validatedToken);

        Assert.Equal(
            SecurityAlgorithms.HmacSha256,
            jwt.Header.Alg);
    }

    [Fact]
    public void Generate_ShouldContainExpectedClaims()
    {
        var options =
            CreateOptions(
                RandomNumberGenerator.GetBytes(
                    32));

        var generator =
            new JwtAccessTokenGenerator(
                options);

        var user =
            CreateUser();

        var sessionId =
            AuthSessionId.New();

        var result =
            generator.Generate(
                user,
                sessionId,
                DateTimeOffset.UtcNow);

        var handler =
            new JwtSecurityTokenHandler
            {
                MapInboundClaims = false
            };

        var token =
            handler.ReadJwtToken(
                result.Token);

        Assert.Equal(
            user.Id.Value.ToString(),
            token.Claims
                .Single(
                    claim =>
                        claim.Type ==
                        JwtRegisteredClaimNames.Sub)
                .Value);

        Assert.Equal(
            user.Email.Value,
            token.Claims
                .Single(
                    claim =>
                        claim.Type ==
                        JwtRegisteredClaimNames.Email)
                .Value);

        Assert.Equal(
            sessionId.Value.ToString(),
            token.Claims
                .Single(
                    claim =>
                        claim.Type == "sid")
                .Value);

        Assert.False(
            string.IsNullOrWhiteSpace(
                token.Claims
                    .Single(
                        claim =>
                            claim.Type ==
                            JwtRegisteredClaimNames.Jti)
                    .Value));
    }

    [Fact]
    public void Generate_ShouldUseConfiguredIssuerAndAudience()
    {
        var options =
            CreateOptions(
                RandomNumberGenerator.GetBytes(
                    32));

        var generator =
            new JwtAccessTokenGenerator(
                options);

        var result =
            generator.Generate(
                CreateUser(),
                AuthSessionId.New(),
                DateTimeOffset.UtcNow);

        var token =
            new JwtSecurityTokenHandler()
                .ReadJwtToken(
                    result.Token);

        Assert.Equal(
            options.Issuer,
            token.Issuer);

        Assert.Contains(
            options.Audience,
            token.Audiences);
    }

    [Fact]
    public void Generate_ShouldUseConfiguredLifetime()
    {
        var options =
            CreateOptions(
                RandomNumberGenerator.GetBytes(
                    32));

        var generator =
            new JwtAccessTokenGenerator(
                options);

        var issuedAt =
            DateTimeOffset.UtcNow;

        var result =
            generator.Generate(
                CreateUser(),
                AuthSessionId.New(),
                issuedAt);

        Assert.Equal(
            issuedAt.AddMinutes(15),
            result.ExpiresAtUtc);

        var token =
            new JwtSecurityTokenHandler()
                .ReadJwtToken(
                    result.Token);

        Assert.Equal(
            result.ExpiresAtUtc.ToUnixTimeSeconds(),
            new DateTimeOffset(
                    token.ValidTo,
                    TimeSpan.Zero)
                .ToUnixTimeSeconds());
    }

    [Fact]
    public void Generate_Twice_ShouldProduceDifferentTokens()
    {
        var generator =
            new JwtAccessTokenGenerator(
                CreateOptions(
                    RandomNumberGenerator.GetBytes(
                        32)));

        var user =
            CreateUser();

        var sessionId =
            AuthSessionId.New();

        var issuedAt =
            DateTimeOffset.UtcNow;

        var first =
            generator.Generate(
                user,
                sessionId,
                issuedAt);

        var second =
            generator.Generate(
                user,
                sessionId,
                issuedAt);

        Assert.NotEqual(
            first.Token,
            second.Token);
    }

    private static JwtOptions CreateOptions(
        byte[] signingKey)
    {
        return new JwtOptions
        {
            Issuer =
                "PGLN.Auth.Tests",

            Audience =
                "PGLN.Auth.Tests.Client",

            SigningKey =
                Convert.ToBase64String(
                    signingKey),

            AccessTokenLifetime =
                TimeSpan.FromMinutes(15)
        };
    }

    private static User CreateUser()
    {
        var now =
            DateTimeOffset.UtcNow;

        var user =
            User.Register(
                UserId.New(),
                PGLN.Auth.Domain.Users.Email.Create(
                    "user@example.com"),
                "hashed-password",
                now.AddDays(-1));

        user.ConfirmEmail(
            now.AddHours(-1));

        user.ClearDomainEvents();

        return user;
    }
}

