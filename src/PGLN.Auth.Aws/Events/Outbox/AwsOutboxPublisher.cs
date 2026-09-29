using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Outbox;

namespace PGLN.Auth.Aws.Events.Outbox;

public sealed class AwsOutboxPublisher
{
    private readonly IServiceScopeFactory _scopeFactory;

    public AwsOutboxPublisher(
        IServiceScopeFactory scopeFactory)
    {
        ArgumentNullException.ThrowIfNull(
            scopeFactory);

        _scopeFactory =
            scopeFactory;
    }

    public async Task<int> PublishAsync(
        string workerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            workerId);

        await using var scope =
            _scopeFactory.CreateAsyncScope();

        var processor =
            scope.ServiceProvider
                .GetRequiredService<IOutboxProcessor>();

        return await processor.ProcessAsync(
            workerId,
            cancellationToken);
    }
}
