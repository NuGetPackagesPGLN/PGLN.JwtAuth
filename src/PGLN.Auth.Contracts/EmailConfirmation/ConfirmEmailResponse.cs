namespace PGLN.Auth.Contracts.EmailConfirmation;

public sealed record ConfirmEmailResponse(
    Guid UserId,
    string Email,
    bool EmailConfirmed);
