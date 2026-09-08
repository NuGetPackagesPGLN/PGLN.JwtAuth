using PGLN.Auth.Contracts.Authentication;

namespace PGLN.Auth.ContractTests.Authentication;

public sealed class LogoutContractTests
{
    [Fact]
    public void LogoutRequest_ShouldExposeRefreshToken()
    {
        var request =
            new LogoutRequest(
                "refresh-token");

        Assert.Equal(
            "refresh-token",
            request.RefreshToken);
    }
}
