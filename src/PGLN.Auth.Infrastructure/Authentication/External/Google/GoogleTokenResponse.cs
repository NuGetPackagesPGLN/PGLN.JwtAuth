using System.Text.Json.Serialization;

namespace PGLN.Auth.Infrastructure.Authentication.External.Google;

internal sealed class GoogleTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } =
        string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }

    [JsonPropertyName("scope")]
    public string? Scope { get; init; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } =
        string.Empty;

    [JsonPropertyName("id_token")]
    public string? IdToken { get; init; }
}
