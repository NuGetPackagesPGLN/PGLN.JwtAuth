using System.Text.Json;
using PGLN.Auth.Contracts.Authentication;

namespace PGLN.Auth.ContractTests.Authentication;

public sealed class RefreshTokenContractTests
{
    [Fact]
    public void RefreshTokenRequest_ShouldExposeOnlyRefreshToken()
    {
        var request =
            new RefreshTokenRequest(
                "raw-refresh-token");

        var json =
            JsonSerializer.Serialize(
                request);

        using var document =
            JsonDocument.Parse(
                json);

        var root =
            document.RootElement;

        Assert.Equal(
            "raw-refresh-token",
            root.GetProperty(
                    "RefreshToken")
                .GetString());

        Assert.False(
            root.TryGetProperty(
                "AccessToken",
                out _));

        Assert.False(
            root.TryGetProperty(
                "Email",
                out _));
    }

    [Fact]
    public void RefreshTokenResponse_ShouldExposeRotatedTokenPair()
    {
        var response =
            new RefreshTokenResponse(
                Guid.NewGuid(),
                "user@example.com",
                "access-token",
                DateTimeOffset.UtcNow.AddMinutes(15),
                "refresh-token",
                DateTimeOffset.UtcNow.AddDays(30));

        var json =
            JsonSerializer.Serialize(
                response);

        using var document =
            JsonDocument.Parse(
                json);

        var root =
            document.RootElement;

        Assert.True(
            root.TryGetProperty(
                "UserId",
                out _));

        Assert.True(
            root.TryGetProperty(
                "Email",
                out _));

        Assert.True(
            root.TryGetProperty(
                "AccessToken",
                out _));

        Assert.True(
            root.TryGetProperty(
                "AccessTokenExpiresAtUtc",
                out _));

        Assert.True(
            root.TryGetProperty(
                "RefreshToken",
                out _));

        Assert.True(
            root.TryGetProperty(
                "RefreshTokenExpiresAtUtc",
                out _));

        Assert.False(
            root.TryGetProperty(
                "Password",
                out _));

        Assert.False(
            root.TryGetProperty(
                "PasswordHash",
                out _));
    }
}
