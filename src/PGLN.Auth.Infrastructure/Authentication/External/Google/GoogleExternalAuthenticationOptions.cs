namespace PGLN.Auth.Infrastructure.Authentication.External.Google;

public sealed class GoogleExternalAuthenticationOptions
{
    public const string SectionName =
        "PGLNAuth:ExternalAuthentication:Google";

    public string ClientId { get; init; } =
        string.Empty;

    public string ClientSecret { get; init; } =
        string.Empty;

    public string TokenEndpoint { get; init; } =
        "https://oauth2.googleapis.com/token";

    public string UserInfoEndpoint { get; init; } =
        "https://openidconnect.googleapis.com/v1/userinfo";

    public string[] AllowedRedirectUris { get; init; } =
        Array.Empty<string>();

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            ClientId);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            ClientSecret);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            TokenEndpoint);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            UserInfoEndpoint);

        if (!Uri.TryCreate(
                TokenEndpoint,
                UriKind.Absolute,
                out var tokenEndpointUri) ||
            tokenEndpointUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                "Google token endpoint must be a valid HTTPS URI.");
        }

        if (!Uri.TryCreate(
                UserInfoEndpoint,
                UriKind.Absolute,
                out var userInfoEndpointUri) ||
            userInfoEndpointUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                "Google user-info endpoint must be a valid HTTPS URI.");
        }

        if (AllowedRedirectUris.Length == 0)
        {
            throw new InvalidOperationException(
                "At least one Google redirect URI must be configured.");
        }

        foreach (var redirectUri in AllowedRedirectUris)
        {
            if (!Uri.TryCreate(
                    redirectUri,
                    UriKind.Absolute,
                    out var parsedRedirectUri))
            {
                throw new InvalidOperationException(
                    $"Google redirect URI '{redirectUri}' is invalid.");
            }

            var isHttps =
                parsedRedirectUri.Scheme ==
                Uri.UriSchemeHttps;

            var isLocalhost =
                parsedRedirectUri.IsLoopback &&
                parsedRedirectUri.Scheme ==
                Uri.UriSchemeHttp;

            if (!isHttps &&
                !isLocalhost)
            {
                throw new InvalidOperationException(
                    $"Google redirect URI '{redirectUri}' must use HTTPS, except for localhost development callbacks.");
            }
        }
    }

    public bool IsRedirectUriAllowed(
        string redirectUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            redirectUri);

        return AllowedRedirectUris.Any(
            allowedRedirectUri =>
                string.Equals(
                    allowedRedirectUri,
                    redirectUri,
                    StringComparison.Ordinal));
    }
}
