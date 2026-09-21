using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Application.Features.ChangeEmail;
using PGLN.Auth.Contracts.ChangeEmail;
using PGLN.Auth.Contracts.Common;

namespace PGLN.Auth.AspNetCore.Endpoints.ChangeEmail;

internal static class ConfirmEmailChangeEndpoint
{
    internal static RouteHandlerBuilder Map(
        IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(
            endpoints);

        return endpoints.MapPost(
            "/confirm-email-change",
            HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        ConfirmEmailChangeRequest request,
        ICommandHandler<
            ConfirmEmailChangeCommand,
            Result> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        ArgumentNullException.ThrowIfNull(
            handler);

        var result =
            await handler.HandleAsync(
                new ConfirmEmailChangeCommand(
                    request.Token),
                cancellationToken);

        if (result.IsFailure)
        {
            return MapFailure(
                result.Error);
        }

        return Results.NoContent();
    }

    private static IResult MapFailure(
        Error error)
    {
        if (error ==
            ConfirmEmailChangeErrors.TokenAlreadyUsed)
        {
            return Results.Conflict(
                new ApiErrorResponse(
                    error.Code,
                    error.Description));
        }

        if (error ==
            ConfirmEmailChangeErrors.EmailAlreadyInUse)
        {
            return Results.Conflict(
                new ApiErrorResponse(
                    error.Code,
                    error.Description));
        }

        if (error ==
            ConfirmEmailChangeErrors.ExpiredToken)
        {
            return Results.Json(
                new ApiErrorResponse(
                    error.Code,
                    error.Description),
                statusCode:
                    StatusCodes.Status410Gone);
        }

        return Results.BadRequest(
            new ApiErrorResponse(
                error.Code,
                error.Description));
    }
}
