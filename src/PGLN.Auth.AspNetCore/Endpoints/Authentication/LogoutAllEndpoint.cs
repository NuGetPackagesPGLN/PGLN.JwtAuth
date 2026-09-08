using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common.Validation;
using PGLN.Auth.Application.Features.LogoutAll;
using PGLN.Auth.Contracts.Authentication;

namespace PGLN.Auth.AspNetCore.Endpoints.Authentication;

internal static class LogoutAllEndpoint
{
    public static RouteHandlerBuilder MapLogoutAll(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/logout-all",
            HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        LogoutAllRequest request,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await dispatcher.SendAsync(
                    new LogoutAllCommand(
                        request.RefreshToken),
                    cancellationToken);

            if (result.IsFailure)
            {
                var statusCode =
                    result.Error.Code switch
                    {
                        "LogoutAll.InvalidToken" =>
                            StatusCodes.Status401Unauthorized,

                        "LogoutAll.ExpiredToken" =>
                            StatusCodes.Status401Unauthorized,

                        "LogoutAll.RevokedToken" =>
                            StatusCodes.Status401Unauthorized,

                        _ =>
                            StatusCodes.Status400BadRequest
                    };

                return Results.Json(
                    new
                    {
                        code =
                            result.Error.Code,

                        description =
                            result.Error.Description
                    },
                    statusCode:
                        statusCode);
            }

            return Results.NoContent();
        }
        catch (CommandValidationException exception)
        {
            return Results.Json(
                new
                {
                    errors =
                        exception.Errors
                },
                statusCode:
                    StatusCodes.Status400BadRequest);
        }
    }
}
