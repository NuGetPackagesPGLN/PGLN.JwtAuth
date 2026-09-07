namespace PGLN.Auth.Application.Abstractions.Authentication;

public sealed record AccessTokenResult(
    string Token,
    DateTimeOffset ExpiresAtUtc);
