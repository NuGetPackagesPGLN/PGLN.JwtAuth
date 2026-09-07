using System.Text.Json;
using PGLN.Auth.Contracts.EmailConfirmation;

namespace PGLN.Auth.ContractTests.EmailConfirmation;

public sealed class ResendEmailConfirmationContractTests
{
    [Fact]
    public void Request_ShouldSerializeEmail()
    {
        var request =
            new ResendEmailConfirmationRequest(
                "user@example.com");

        var json =
            JsonSerializer.Serialize(
                request,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy =
                        JsonNamingPolicy.CamelCase
                });

        using var document =
            JsonDocument.Parse(
                json);

        Assert.Equal(
            "user@example.com",
            document
                .RootElement
                .GetProperty("email")
                .GetString());
    }

    [Fact]
    public void Response_ShouldExposeOnlyPublicMessage()
    {
        var response =
            new ResendEmailConfirmationResponse(
                "Generic confirmation message.");

        var json =
            JsonSerializer.Serialize(
                response,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy =
                        JsonNamingPolicy.CamelCase
                });

        using var document =
            JsonDocument.Parse(
                json);

        var root =
            document.RootElement;

        Assert.Single(
            root.EnumerateObject());

        Assert.Equal(
            "Generic confirmation message.",
            root
                .GetProperty("message")
                .GetString());
    }
}


