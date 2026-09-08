namespace PGLN.Auth.Contracts.Authentication;

public sealed record LogoutAllRequest(
    string RefreshToken);
