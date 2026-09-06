using PGLN.Auth.Application.Abstractions.Email;

namespace PGLN.Auth.IntegrationTests.Http;

internal sealed class HttpTestEmailSender
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

        _messages.Add(
            message);

        return Task.CompletedTask;
    }
}
