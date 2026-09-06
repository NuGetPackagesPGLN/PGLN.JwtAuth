namespace PGLN.Auth.Contracts.Common;

public sealed record ValidationErrorResponse(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]> Errors);
