namespace PGLN.Auth.Contracts.ChangeEmail;

public sealed record RequestEmailChangeRequest(
    string NewEmail,
    string CurrentPassword);
