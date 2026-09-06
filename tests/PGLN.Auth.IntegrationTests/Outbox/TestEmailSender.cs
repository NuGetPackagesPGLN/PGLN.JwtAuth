using PGLN.Auth.Application.Abstractions.Email;

namespace PGLN.Auth.IntegrationTests.Outbox;

internal sealed class TestEmailSender
    : IEmailSender
{
    private readonly List<EmailMessage> _messages = [];

    public IReadOnlyCollection<EmailMessage> Messages =>
        _messages.AsReadOnly();

    public bool ThrowOnSend { get; set; }

    public Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (ThrowOnSend)
        {
            throw new InvalidOperationException(
                "Simulated email provider failure.");
        }

        _messages.Add(message);

        return Task.CompletedTask;
    }
}
