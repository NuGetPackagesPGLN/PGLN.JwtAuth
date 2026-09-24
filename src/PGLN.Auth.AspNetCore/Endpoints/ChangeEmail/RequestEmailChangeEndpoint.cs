using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Application.Features.ChangeEmail;
using PGLN.Auth.Contracts.ChangeEmail;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.AspNetCore.Endpoints.ChangeEmail;

internal static class RequestEmailChangeEndpoint
{
    public static RouteHandlerBuilder MapRequestEmailChange(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(
            endpoints);

        return endpoints.MapPost(
            "/change-email",
            HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        RequestEmailChangeRequest request,
        HttpContext httpContext,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        ArgumentNullException.ThrowIfNull(
            httpContext);

        var userIdValue =
            httpContext.User.FindFirst(
                "sub")
                ?.Value;

        if (string.IsNullOrWhiteSpace(
                userIdValue) ||
            !Guid.TryParse(
                userIdValue,
                out var userId))
        {
            return Results.Json(
                new
                {
                    code =
                        "ChangeEmail.Unauthorized",

                    description =
                        "The authenticated user could not be identified."
                },
                statusCode:
                    StatusCodes.Status401Unauthorized);
        }

        var result =
            await dispatcher.SendAsync(
                new RequestEmailChangeCommand(
                    new UserId(userId),
                    request.NewEmail,
                    request.CurrentPassword),
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
                "ChangeEmail.UserNotFound" =>
                    StatusCodes.Status401Unauthorized,

                "ChangeEmail.EmailAlreadyInUse" =>
                    StatusCodes.Status409Conflict,

                "ChangeEmail.InvalidCurrentPassword" =>
                    StatusCodes.Status400BadRequest,

                "ChangeEmail.SameEmail" =>
                    StatusCodes.Status400BadRequest,

                "ChangeEmail.InvalidEmail" =>
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
