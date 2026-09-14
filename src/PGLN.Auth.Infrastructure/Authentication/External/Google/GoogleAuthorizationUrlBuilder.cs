using System.Web;
using PGLN.Auth.Application.Abstractions.ExternalAuthentication;
using PGLN.Auth.Domain.ExternalLogins;

namespace PGLN.Auth.Infrastructure.Authentication.External.Google;

public sealed class GoogleAuthorizationUrlBuilder
    : IExternalAuthorizationUrlBuilder
{
    private const string AuthorizationEndpoint =
        "https://accounts.google.com/o/oauth2/v2/auth";

    public ExternalLoginProvider Provider =>
        ExternalLoginProvider.Google;

    private readonly GoogleExternalAuthenticationOptions _options;

    public GoogleAuthorizationUrlBuilder(
        GoogleExternalAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(
            options);

        options.Validate();

        _options = options;
    }

    public string Build(
        string redirectUri,
        string state,
        string codeChallenge)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            redirectUri);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            state);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            codeChallenge);

        if (!_options.IsRedirectUriAllowed(
                redirectUri))
        {
            throw new InvalidOperationException(
                "The Google redirect URI is not allowed.");
        }

        var query =
            HttpUtility.ParseQueryString(
                string.Empty);

        query["client_id"] =
            _options.ClientId;

        query["redirect_uri"] =
            redirectUri;

        query["response_type"] =
            "code";

        query["scope"] =
            "openid email profile";

        query["state"] =
            state;

        query["code_challenge"] =
            codeChallenge;

        query["code_challenge_method"] =
            "S256";

        query["include_granted_scopes"] =
            "true";

        return
            $"{AuthorizationEndpoint}?{query}";
    }
}
