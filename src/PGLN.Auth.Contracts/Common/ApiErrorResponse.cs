namespace PGLN.Auth.Contracts.Common;

public sealed record ApiErrorResponse(
    string Code,
    string Message);
