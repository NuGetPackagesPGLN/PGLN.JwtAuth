using PGLN.Auth.Application.Abstractions.ExternalAuthentication;
using PGLN.Auth.Domain.ExternalLogins;

namespace PGLN.Auth.Application.Tests.TestDoubles;

public sealed class FakeExternalIdentityProvider
    : IExternalIdentityProvider
{
    public FakeExternalIdentityProvider(
        ExternalLoginProvider provider)
    {
        Provider = provider;

        Identity =
            new ExternalIdentity(
                provider,
                "external-subject",
                "external@example.com",
                true,
                "External User");
    }

    public ExternalLoginProvider Provider { get; }

    public ExternalIdentity Identity { get; set; }

    public ExternalIdentityProviderException? ExceptionToThrow { get; set; }

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
        GetIdentityCallCount++;

        LastAuthorizationCode =
            authorizationCode;

        LastRedirectUri =
            redirectUri;

        LastCodeVerifier =
            codeVerifier;

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        return Task.FromResult(
            Identity);
    }
}

