using PGLN.Auth.Application.Abstractions.Email;

namespace PGLN.Auth.Application.Tests.TestDoubles;

internal sealed class FakeEmailSender
    : IEmailSender
{
    private readonly List<EmailMessage> _messages = [];

    public IReadOnlyCollection<EmailMessage> Messages =>
        _messages.AsReadOnly();

    public Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(message);

        _messages.Add(message);

        return Task.CompletedTask;
    }
}
