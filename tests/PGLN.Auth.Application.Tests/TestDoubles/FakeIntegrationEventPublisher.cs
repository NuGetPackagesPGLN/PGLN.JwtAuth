using PGLN.Auth.Application.Abstractions.Events;

namespace PGLN.Auth.Application.Tests.TestDoubles;

internal sealed class FakeIntegrationEventPublisher
    : IIntegrationEventPublisher
{
    private readonly List<IIntegrationEvent> _events = [];

    public IReadOnlyCollection<IIntegrationEvent> Events =>
        _events.AsReadOnly();

    public Task PublishAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        cancellationToken.ThrowIfCancellationRequested();

        _events.Add(integrationEvent);

        return Task.CompletedTask;
    }
}
