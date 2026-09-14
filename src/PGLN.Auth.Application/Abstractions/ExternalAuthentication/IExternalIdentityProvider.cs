using PGLN.Auth.Domain.ExternalLogins;

namespace PGLN.Auth.Application.Abstractions.ExternalAuthentication;

public interface IExternalIdentityProvider
{
    ExternalLoginProvider Provider { get; }

    Task<ExternalIdentity> GetIdentityAsync(
        string authorizationCode,
        string redirectUri,
        string codeVerifier,
        CancellationToken cancellationToken = default);
}
