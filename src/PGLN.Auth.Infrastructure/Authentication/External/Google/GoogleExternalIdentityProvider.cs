using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PGLN.Auth.Application.Abstractions.ExternalAuthentication;
using PGLN.Auth.Domain.ExternalLogins;

namespace PGLN.Auth.Infrastructure.Authentication.External.Google;

public sealed class GoogleExternalIdentityProvider
    : IExternalIdentityProvider
{
    private readonly HttpClient _httpClient;
    private readonly GoogleExternalAuthenticationOptions _options;

    public GoogleExternalIdentityProvider(
        HttpClient httpClient,
        GoogleExternalAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(
            httpClient);

        ArgumentNullException.ThrowIfNull(
            options);

        options.Validate();

        _httpClient =
            httpClient;

        _options =
            options;
    }

    public ExternalLoginProvider Provider =>
        ExternalLoginProvider.Google;

    public async Task<ExternalIdentity> GetIdentityAsync(
        string authorizationCode,
        string redirectUri,
        string codeVerifier,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            authorizationCode);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            redirectUri);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            codeVerifier);

        var tokenResponse =
            await ExchangeAuthorizationCodeAsync(
                authorizationCode,
                redirectUri,
                codeVerifier,
                cancellationToken);

        var userInfo =
            await GetUserInfoAsync(
                tokenResponse.AccessToken,
                cancellationToken);

        if (string.IsNullOrWhiteSpace(
                userInfo.Subject))
        {
            throw new ExternalIdentityProviderException(
                "Google did not return a valid subject identifier.");
        }

        return new ExternalIdentity(
            ExternalLoginProvider.Google,
            userInfo.Subject,
            userInfo.Email,
            userInfo.EmailVerified,
            userInfo.Name);
    }

    private async Task<GoogleTokenResponse> ExchangeAuthorizationCodeAsync(
        string authorizationCode,
        string redirectUri,
        string codeVerifier,
        CancellationToken cancellationToken)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                _options.TokenEndpoint);

        request.Content =
            new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["code"] =
                        authorizationCode,

                    ["client_id"] =
                        _options.ClientId,

                    ["client_secret"] =
                        _options.ClientSecret,

                    ["redirect_uri"] =
                        redirectUri,

                    ["grant_type"] =
                        "authorization_code",

                    ["code_verifier"] =
                        codeVerifier
                });

        using var response =
            await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new ExternalIdentityProviderException(
                "Google authorization-code exchange failed.");
        }

        var tokenResponse =
            await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(
                cancellationToken: cancellationToken);

        if (tokenResponse is null ||
            string.IsNullOrWhiteSpace(
                tokenResponse.AccessToken))
        {
            throw new ExternalIdentityProviderException(
                "Google did not return a valid access token.");
        }

        return tokenResponse;
    }

    private async Task<GoogleUserInfoResponse> GetUserInfoAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                _options.UserInfoEndpoint);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        using var response =
            await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new ExternalIdentityProviderException(
                "Google user-info request failed.");
        }

        var userInfo =
            await response.Content.ReadFromJsonAsync<GoogleUserInfoResponse>(
                cancellationToken: cancellationToken);

        if (userInfo is null)
        {
            throw new ExternalIdentityProviderException(
                "Google returned an invalid user-info response.");
        }

        return userInfo;
    }
}


