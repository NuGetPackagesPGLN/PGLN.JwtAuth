using System.Text.Json.Serialization;

namespace PGLN.Auth.Infrastructure.Authentication.External.Google;

internal sealed class GoogleUserInfoResponse
{
    [JsonPropertyName("sub")]
    public string Subject { get; init; } =
        string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("email_verified")]
    public bool EmailVerified { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("given_name")]
    public string? GivenName { get; init; }

    [JsonPropertyName("family_name")]
    public string? FamilyName { get; init; }

    [JsonPropertyName("picture")]
    public string? Picture { get; init; }
}
