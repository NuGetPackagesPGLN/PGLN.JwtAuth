namespace PGLN.Auth.Application.Common.Validation;

public sealed class CommandValidationException : Exception
{
    public CommandValidationException(
        IReadOnlyCollection<ApplicationValidationError> errors)
        : base("One or more validation errors occurred.")
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.Count == 0)
        {
            throw new ArgumentException(
                "At least one validation error is required.",
                nameof(errors));
        }

        Errors = errors;
    }

    public IReadOnlyCollection<ApplicationValidationError> Errors { get; }
}
