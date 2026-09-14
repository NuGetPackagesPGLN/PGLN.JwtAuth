using PGLN.Auth.Domain.ExternalLogins;

namespace PGLN.Auth.Application.Abstractions.ExternalAuthentication;

public interface IExternalAuthorizationUrlBuilder
{
    ExternalLoginProvider Provider { get; }

    string Build(
        string redirectUri,
        string state,
        string codeChallenge);
}
