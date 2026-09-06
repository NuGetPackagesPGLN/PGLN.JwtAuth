namespace PGLN.Auth.Contracts.Registration;

public sealed record RegisterResponse(
    Guid UserId,
    string Email,
    bool EmailConfirmed);
