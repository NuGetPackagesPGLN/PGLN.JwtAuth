using System.Text.Json;
using PGLN.Auth.Contracts.Authentication;

namespace PGLN.Auth.ContractTests.Authentication;

public sealed class LogoutAllContractTests
{
    [Fact]
    public void LogoutAllRequest_ShouldSerializeExpectedShape()
    {
        var request =
            new LogoutAllRequest(
                "refresh-token");

        var json =
            JsonSerializer.Serialize(
                request);

        Assert.Contains(
            "\"RefreshToken\":\"refresh-token\"",
            json);
    }
}
