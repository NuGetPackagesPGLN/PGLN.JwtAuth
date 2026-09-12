using System.Text.Json;
using PGLN.Auth.Contracts.Authentication;

namespace PGLN.Auth.ContractTests.Authentication;

public sealed class LoginContractTests
{
    [Fact]
    public void LoginRequest_ShouldSerializeExpectedShape()
    {
        var request =
            new LoginRequest(
                "user@example.com",
                "SecretPassword123!",
                "device-hash-123",
                "Chrome on Windows");

        var json =
            JsonSerializer.Serialize(
                request);

        using var document =
            JsonDocument.Parse(
                json);

        var root =
            document.RootElement;

        Assert.Equal(
            "user@example.com",
            root.GetProperty("Email")
                .GetString());

        Assert.Equal(
            "SecretPassword123!",
            root.GetProperty("Password")
                .GetString());

        Assert.Equal(
            "device-hash-123",
            root.GetProperty("DeviceIdHash")
                .GetString());

        Assert.Equal(
            "Chrome on Windows",
            root.GetProperty("DeviceName")
                .GetString());

        Assert.Equal(
            4,
            root.EnumerateObject()
                .Count());
    }

    [Fact]
    public void LoginResponse_ShouldSerializeExpectedPublicFields()
    {
        var response =
            new LoginResponse(
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
