namespace PGLN.Auth.Contracts.Authentication;

public sealed record ResetPasswordRequest(
    string Email,
    string Token,
    string NewPassword);
