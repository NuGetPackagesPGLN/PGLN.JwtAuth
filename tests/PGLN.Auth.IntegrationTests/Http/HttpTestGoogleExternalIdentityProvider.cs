using PGLN.Auth.Application.Abstractions.ExternalAuthentication;
using PGLN.Auth.Domain.ExternalLogins;

namespace PGLN.Auth.IntegrationTests.Http;

internal sealed class HttpTestGoogleExternalIdentityProvider
    : IExternalIdentityProvider
{
    public ExternalLoginProvider Provider =>
        ExternalLoginProvider.Google;

    public ExternalIdentity Identity { get; set; } =
        new(
            ExternalLoginProvider.Google,
            "integration-test-google-subject",
            "google-user@example.com",
            true,
            "Google Integration User");

    public int GetIdentityCallCount { get; private set; }

    public string? LastAuthorizationCode { get; private set; }

    public string? LastRedirectUri { get; private set; }

    public string? LastCodeVerifier { get; private set; }

    public Task<ExternalIdentity> GetIdentityAsync(
        string authorizationCode,
        string redirectUri,
        string codeVerifier,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        GetIdentityCallCount++;

        LastAuthorizationCode =
            authorizationCode;

        LastRedirectUri =
            redirectUri;

        LastCodeVerifier =
            codeVerifier;

        return Task.FromResult(
            Identity);
    }
}
