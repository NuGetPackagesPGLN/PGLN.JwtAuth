using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Application.Features.Registration;
using PGLN.Auth.Contracts.Common;
using PGLN.Auth.Contracts.Registration;

namespace PGLN.Auth.AspNetCore.Endpoints.Registration;

internal static class RegisterEndpoint
{
    internal static RouteHandlerBuilder Map(
        IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(
            endpoints);

        return endpoints.MapPost(
            "/register",
            HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        RegisterRequest request,
        ICommandHandler<
            RegisterCommand,
            Result<RegisterResult>> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        ArgumentNullException.ThrowIfNull(
            handler);

        var result =
            await handler.HandleAsync(
                new RegisterCommand(
                    request.Email,
                    request.Password),
                cancellationToken);

        if (result.IsFailure)
        {
            return MapFailure(
                result.Error);
        }

        var response =
            new RegisterResponse(
                result.Value.UserId.Value,
                result.Value.Email,
                result.Value.EmailConfirmed);

        return Results.Created(
            $"/api/auth/users/{response.UserId}",
            response);
    }

    private static IResult MapFailure(
        Error error)
    {
        if (error ==
            RegistrationErrors.EmailAlreadyExists)
        {
            return Results.Conflict(
                new ApiErrorResponse(
                    error.Code,
                    error.Description));
        }

        return Results.BadRequest(
            new ApiErrorResponse(
                error.Code,
                error.Description));
    }
}
