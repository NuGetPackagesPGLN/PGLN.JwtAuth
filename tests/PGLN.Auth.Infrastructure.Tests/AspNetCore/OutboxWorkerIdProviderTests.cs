using PGLN.Auth.AspNetCore.Outbox;

namespace PGLN.Auth.Infrastructure.Tests.AspNetCore;

public sealed class OutboxWorkerIdProviderTests
{
    [Fact]
    public void WorkerId_ShouldContainConfiguredPrefix()
    {
        var provider =
            new OutboxWorkerIdProvider(
                new OutboxBackgroundWorkerOptions
                {
                    WorkerIdPrefix =
                        "test-worker"
                });

        Assert.StartsWith(
            "test-worker-",
            provider.WorkerId);
    }

    [Fact]
    public void SeparateProviders_ShouldGenerateDifferentWorkerIds()
    {
        var options =
            new OutboxBackgroundWorkerOptions();

        var first =
            new OutboxWorkerIdProvider(
                options);

        var second =
            new OutboxWorkerIdProvider(
                options);

        Assert.NotEqual(
            first.WorkerId,
            second.WorkerId);
    }
}
