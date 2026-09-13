using Microsoft.AspNetCore.DataProtection;
using PGLN.Auth.Application.Abstractions.Events;

namespace PGLN.Auth.Infrastructure.Events;

public sealed class DataProtectionIntegrationEventPayloadProtector
    : IIntegrationEventPayloadProtector
{
    private const string Purpose =
        "PGLN.Auth.Outbox.IntegrationEvents.v1";

    private readonly IDataProtector _protector;

    public DataProtectionIntegrationEventPayloadProtector(
        IDataProtectionProvider dataProtectionProvider)
    {
        ArgumentNullException.ThrowIfNull(
            dataProtectionProvider);

        _protector =
            dataProtectionProvider.CreateProtector(
                Purpose);
    }

    public string Protect(
        string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            plaintext);

        return _protector.Protect(
            plaintext);
    }

    public string Unprotect(
        string protectedPayload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            protectedPayload);

        return _protector.Unprotect(
            protectedPayload);
    }
}
