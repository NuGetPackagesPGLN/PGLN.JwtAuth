namespace PGLN.Auth.Application.Abstractions.Events;

public interface IIntegrationEventPayloadProtector
{
    string Protect(string plaintext);

    string Unprotect(string protectedPayload);
}
