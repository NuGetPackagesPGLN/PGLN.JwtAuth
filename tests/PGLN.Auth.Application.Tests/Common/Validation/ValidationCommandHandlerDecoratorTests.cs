using FluentValidation;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common.Validation;

namespace PGLN.Auth.Application.Tests.Common.Validation;

public sealed class ValidationCommandHandlerDecoratorTests
{
    [Fact]
    public async Task HandleAsync_WithNoValidators_ShouldExecuteHandler()
    {
        var handler =
            new TestCommandHandler();

        var decorator =
            new ValidationCommandHandlerDecorator<TestCommand, string>(
                handler,
                []);

        var result =
            await decorator.HandleAsync(
                new TestCommand("valid"));

        Assert.Equal("handled", result);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_ShouldExecuteHandler()
    {
        var handler =
            new TestCommandHandler();

        IValidator<TestCommand>[] validators =
        [
            new TestCommandValidator()
        ];

        var decorator =
            new ValidationCommandHandlerDecorator<TestCommand, string>(
                handler,
                validators);

        var result =
            await decorator.HandleAsync(
                new TestCommand("valid"));

        Assert.Equal("handled", result);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidCommand_ShouldNotExecuteHandler()
    {
        var handler =
            new TestCommandHandler();

        IValidator<TestCommand>[] validators =
        [
            new TestCommandValidator()
        ];

        var decorator =
            new ValidationCommandHandlerDecorator<TestCommand, string>(
                handler,
                validators);

        var exception =
            await Assert.ThrowsAsync<CommandValidationException>(
                () =>
                    decorator.HandleAsync(
                        new TestCommand(string.Empty)));

        Assert.Equal(0, handler.CallCount);

        var error =
            Assert.Single(exception.Errors);

        Assert.Equal(
            nameof(TestCommand.Value),
            error.PropertyName);

        Assert.Equal(
            "Value is required.",
            error.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_WithMultipleValidators_ShouldCombineErrors()
    {
        var handler =
            new TestCommandHandler();

        IValidator<TestCommand>[] validators =
        [
            new TestCommandValidator(),
            new MinimumLengthValidator()
        ];

        var decorator =
            new ValidationCommandHandlerDecorator<TestCommand, string>(
                handler,
                validators);

        var exception =
            await Assert.ThrowsAsync<CommandValidationException>(
                () =>
                    decorator.HandleAsync(
                        new TestCommand(string.Empty)));

        Assert.Equal(0, handler.CallCount);
        Assert.Equal(2, exception.Errors.Count);
    }

    private sealed record TestCommand(
        string Value)
        : ICommand<string>;

    private sealed class TestCommandHandler
        : ICommandHandler<TestCommand, string>
    {
        public int CallCount { get; private set; }

        public Task<string> HandleAsync(
            TestCommand command,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CallCount++;

            return Task.FromResult("handled");
        }
    }

    private sealed class TestCommandValidator
        : AbstractValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(command => command.Value)
                .NotEmpty()
                .WithMessage("Value is required.");
        }
    }

    private sealed class MinimumLengthValidator
        : AbstractValidator<TestCommand>
    {
        public MinimumLengthValidator()
        {
            RuleFor(command => command.Value)
                .MinimumLength(5)
                .WithMessage(
                    "Value must contain at least 5 characters.");
        }
    }
}
