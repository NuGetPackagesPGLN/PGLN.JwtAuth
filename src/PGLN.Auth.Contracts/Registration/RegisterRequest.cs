namespace PGLN.Auth.Contracts.Registration;

public sealed record RegisterRequest(
    string Email,
    string Password);
