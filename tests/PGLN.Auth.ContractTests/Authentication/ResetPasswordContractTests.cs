using System.Text.Json;
using PGLN.Auth.Contracts.Authentication;

namespace PGLN.Auth.ContractTests.Authentication;

public sealed class ResetPasswordContractTests
{
    [Fact]
    public void ResetPasswordRequest_ShouldSerializeExpectedShape()
    {
        var request =
            new ResetPasswordRequest(
                "user@example.com",
                "reset-token",
                "NewPassword123!");

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
            root.GetProperty(
                "Email")
                .GetString());

        Assert.Equal(
            "reset-token",
            root.GetProperty(
                "Token")
                .GetString());

        Assert.Equal(
            "NewPassword123!",
            root.GetProperty(
                "NewPassword")
                .GetString());
    }

    [Fact]
    public void ResetPasswordRequest_ShouldDeserializeExpectedShape()
    {
        const string json =
            """
            {
              "email": "user@example.com",
              "token": "reset-token",
              "newPassword": "NewPassword123!"
            }
            """;

        var request =
            JsonSerializer.Deserialize<ResetPasswordRequest>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive =
                        true
                });

        Assert.NotNull(
            request);

        Assert.Equal(
            "user@example.com",
            request.Email);

        Assert.Equal(
            "reset-token",
            request.Token);

        Assert.Equal(
            "NewPassword123!",
            request.NewPassword);
    }
}
