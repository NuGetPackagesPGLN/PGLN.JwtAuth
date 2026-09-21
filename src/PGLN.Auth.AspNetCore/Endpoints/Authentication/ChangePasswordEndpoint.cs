using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Application.Features.ChangePassword;
using PGLN.Auth.Contracts.Authentication;

namespace PGLN.Auth.AspNetCore.Endpoints.Authentication;

internal static class ChangePasswordEndpoint
{
    public static RouteHandlerBuilder MapChangePassword(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(
            endpoints);

        return endpoints.MapPost(
            "/change-password",
            HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        ChangePasswordRequest request,
        HttpContext httpContext,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        ArgumentNullException.ThrowIfNull(
            httpContext);

        var userId =
            httpContext.User.FindFirst(
                "sub")
                ?.Value;

        if (string.IsNullOrWhiteSpace(
                userId))
        {
            return Results.Json(
                new
                {
                    code =
                        "ChangePassword.Unauthorized",

                    description =
                        "The authenticated user could not be identified."
                },
                statusCode:
                    StatusCodes.Status401Unauthorized);
        }

        var result =
            await dispatcher.SendAsync(
                new ChangePasswordCommand(
                    userId,
                    request.CurrentPassword,
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
                "ChangePassword.UserNotFound" =>
                    StatusCodes.Status401Unauthorized,

                "ChangePassword.InvalidCurrentPassword" =>
                    StatusCodes.Status400BadRequest,

                "ChangePassword.SamePassword" =>
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
