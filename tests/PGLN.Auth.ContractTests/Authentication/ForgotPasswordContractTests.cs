using System.Text.Json;
using PGLN.Auth.Contracts.Authentication;

namespace PGLN.Auth.ContractTests.Authentication;

public sealed class ForgotPasswordContractTests
{
    [Fact]
    public void ForgotPasswordRequest_ShouldSerializeExpectedShape()
    {
        var request =
            new ForgotPasswordRequest(
                "user@example.com");

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
    }

    [Fact]
    public void ForgotPasswordRequest_ShouldDeserializeExpectedShape()
    {
        const string json =
            """
            {
              "email": "user@example.com"
            }
            """;

        var request =
            JsonSerializer.Deserialize<ForgotPasswordRequest>(
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
    }
}
