using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Application.Features.ResetPassword;
using PGLN.Auth.Contracts.Authentication;

namespace PGLN.Auth.AspNetCore.Endpoints.Authentication;

internal static class ResetPasswordEndpoint
{
    public static RouteHandlerBuilder MapResetPassword(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(
            endpoints);

        return endpoints.MapPost(
            "/reset-password",
            HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        ResetPasswordRequest request,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        var result =
            await dispatcher.SendAsync(
                new ResetPasswordCommand(
                    request.Email,
                    request.Token,
                    request.NewPassword),
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
        var statusCode =
            error.Code switch
            {
                "ResetPassword.InvalidToken" =>
                    StatusCodes.Status400BadRequest,

                "ResetPassword.ExpiredToken" =>
                    StatusCodes.Status410Gone,

                "ResetPassword.UsedToken" =>
                    StatusCodes.Status409Conflict,

                "ResetPassword.InvalidRequest" =>
                    StatusCodes.Status400BadRequest,

                _ =>
                    StatusCodes.Status400BadRequest
            };

        return Results.Json(
            new
            {
                code =
                    error.Code,

                description =
                    error.Description
            },
            statusCode:
                statusCode);
    }
}
