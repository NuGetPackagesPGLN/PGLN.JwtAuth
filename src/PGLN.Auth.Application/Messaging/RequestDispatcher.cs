using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Messaging;

namespace PGLN.Auth.Application.Messaging;

public sealed class RequestDispatcher
    : IRequestDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public RequestDispatcher(
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(
            serviceProvider);

        _serviceProvider =
            serviceProvider;
    }

    public Task<TResponse> SendAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        var commandType =
            command.GetType();

        var handlerType =
            typeof(ICommandHandler<,>)
                .MakeGenericType(
                    commandType,
                    typeof(TResponse));

        var handler =
            _serviceProvider
                .GetRequiredService(
                    handlerType);

        var method =
            handlerType.GetMethod(
                "HandleAsync")
            ?? throw new InvalidOperationException(
                $"Could not find HandleAsync on '{handlerType.FullName}'.");

        var task =
            method.Invoke(
                handler,
                new object?[]
                {
                    command,
                    cancellationToken
                });

        return task as Task<TResponse>
            ?? throw new InvalidOperationException(
                $"Handler '{handlerType.FullName}' did not return Task<{typeof(TResponse).Name}>.");
    }

    public Task<TResponse> QueryAsync<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            query);

        var queryType =
            query.GetType();

        var handlerType =
            typeof(IQueryHandler<,>)
                .MakeGenericType(
                    queryType,
                    typeof(TResponse));

        var handler =
            _serviceProvider
                .GetRequiredService(
                    handlerType);

        var method =
            handlerType.GetMethod(
                "HandleAsync")
            ?? throw new InvalidOperationException(
                $"Could not find HandleAsync on '{handlerType.FullName}'.");

        var task =
            method.Invoke(
                handler,
                new object?[]
                {
                    query,
                    cancellationToken
                });

        return task as Task<TResponse>
            ?? throw new InvalidOperationException(
                $"Handler '{handlerType.FullName}' did not return Task<{typeof(TResponse).Name}>.");
    }
}

