using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Features.Logout;
using PGLN.Auth.Contracts.Authentication;

namespace PGLN.Auth.AspNetCore.Endpoints.Authentication;

internal static class LogoutEndpoint
{
    public static RouteHandlerBuilder MapLogout(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/logout",
            HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        LogoutRequest request,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result =
            await dispatcher.SendAsync(
                new LogoutCommand(
                    request.RefreshToken),
                cancellationToken);

        if (result.IsFailure)
        {
            return Results.Json(
                new
                {
                    code =
                        result.Error.Code,

                    description =
                        result.Error.Description
                },
                statusCode:
                    StatusCodes.Status400BadRequest);
        }

        return Results.NoContent();
    }
}
