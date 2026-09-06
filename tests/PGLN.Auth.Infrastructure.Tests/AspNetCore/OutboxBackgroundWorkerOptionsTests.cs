using PGLN.Auth.AspNetCore.Outbox;

namespace PGLN.Auth.Infrastructure.Tests.AspNetCore;

public sealed class OutboxBackgroundWorkerOptionsTests
{
    [Fact]
    public void Validate_WithDefaults_ShouldSucceed()
    {
        var options =
            new OutboxBackgroundWorkerOptions();

        options.Validate();
    }

    [Fact]
    public void Validate_WithZeroPollInterval_ShouldThrow()
    {
        var options =
            new OutboxBackgroundWorkerOptions
            {
                PollInterval =
                    TimeSpan.Zero
            };

        Assert.Throws<
            InvalidOperationException>(
            options.Validate);
    }

    [Fact]
    public void Validate_WithEmptyWorkerPrefix_ShouldThrow()
    {
        var options =
            new OutboxBackgroundWorkerOptions
            {
                WorkerIdPrefix =
                    string.Empty
            };

        Assert.Throws<
            InvalidOperationException>(
            options.Validate);
    }
}
