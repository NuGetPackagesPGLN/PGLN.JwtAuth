namespace PGLN.Auth.Contracts.Authentication;

public sealed record LogoutRequest(
    string RefreshToken);
