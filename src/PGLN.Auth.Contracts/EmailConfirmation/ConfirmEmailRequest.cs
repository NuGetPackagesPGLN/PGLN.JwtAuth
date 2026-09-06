namespace PGLN.Auth.Contracts.EmailConfirmation;

public sealed record ConfirmEmailRequest(
    string Token);
