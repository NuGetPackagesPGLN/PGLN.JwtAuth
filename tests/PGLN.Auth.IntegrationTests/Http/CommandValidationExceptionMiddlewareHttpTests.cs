namespace PGLN.Auth.IntegrationTests.Http;

public sealed class CommandValidationExceptionMiddlewareHttpTests
{
    [Fact]
    public async Task Middleware_WithUnrelatedException_ShouldRethrowException()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    client.GetAsync(
                        "/test/unhandled-exception"));

        Assert.Equal(
            "Test exception.",
            exception.Message);
    }
}
