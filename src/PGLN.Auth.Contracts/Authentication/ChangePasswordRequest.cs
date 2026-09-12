namespace PGLN.Auth.Contracts.Authentication;

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);
