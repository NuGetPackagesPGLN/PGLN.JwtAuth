namespace PGLN.Auth.Application.Common.Validation;

public sealed record ApplicationValidationError(
    string PropertyName,
    string ErrorMessage);
