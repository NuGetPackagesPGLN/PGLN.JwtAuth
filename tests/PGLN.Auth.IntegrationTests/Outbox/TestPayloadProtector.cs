using PGLN.Auth.Application.Abstractions.Events;

namespace PGLN.Auth.IntegrationTests.Outbox;

internal sealed class TestPayloadProtector
    : IIntegrationEventPayloadProtector
{
    private const string Prefix =
        "PROTECTED::";

    public string Protect(
        string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        var bytes =
            System.Text.Encoding.UTF8.GetBytes(
                plaintext);

        return Prefix +
               Convert.ToBase64String(bytes);
    }

    public string Unprotect(
        string protectedPayload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            protectedPayload);

        if (!protectedPayload.StartsWith(
            Prefix,
            StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Payload is not protected.");
        }

        var encoded =
            protectedPayload[Prefix.Length..];

        var bytes =
            Convert.FromBase64String(
                encoded);

        return System.Text.Encoding.UTF8.GetString(
            bytes);
    }
}
