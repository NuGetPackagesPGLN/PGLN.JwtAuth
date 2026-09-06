using FluentValidation;
using PGLN.Auth.Application.Common.Validation;

namespace PGLN.Auth.Application.Abstractions.Messaging;

public sealed class ValidationCommandHandlerDecorator<TCommand, TResponse>
    : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    private readonly ICommandHandler<TCommand, TResponse> _innerHandler;
    private readonly IReadOnlyCollection<IValidator<TCommand>> _validators;

    public ValidationCommandHandlerDecorator(
        ICommandHandler<TCommand, TResponse> innerHandler,
        IEnumerable<IValidator<TCommand>> validators)
    {
        ArgumentNullException.ThrowIfNull(innerHandler);
        ArgumentNullException.ThrowIfNull(validators);

        _innerHandler = innerHandler;
        _validators = validators.ToArray();
    }

    public async Task<TResponse> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_validators.Count == 0)
        {
            return await _innerHandler.HandleAsync(
                command,
                cancellationToken);
        }

        var validationContext =
            new ValidationContext<TCommand>(command);

        var validationTasks =
            _validators.Select(
                validator =>
                    validator.ValidateAsync(
                        validationContext,
                        cancellationToken));

        var validationResults =
            await Task.WhenAll(validationTasks);

        var errors =
            validationResults
                .SelectMany(result => result.Errors)
                .Where(failure => failure is not null)
                .Select(
                    failure =>
                        new ApplicationValidationError(
                            failure.PropertyName,
                            failure.ErrorMessage))
                .Distinct()
                .ToArray();

        if (errors.Length > 0)
        {
            throw new CommandValidationException(errors);
        }

        return await _innerHandler.HandleAsync(
            command,
            cancellationToken);
    }
}
