using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Features.Login;
using PGLN.Auth.Contracts.Authentication;

namespace PGLN.Auth.AspNetCore.Endpoints.Authentication;

public static class LoginEndpoint
{
    public static RouteHandlerBuilder MapLoginEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/login",
            HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        LoginRequest request,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result =
            await dispatcher.SendAsync(
                new LoginCommand(
                    request.Email,
                    request.Password),
                cancellationToken);

        if (result.IsSuccess)
        {
            var value =
                result.Value;

            return Results.Ok(
                new LoginResponse(
                    value.UserId,
                    value.Email,
                    value.AccessToken,
                    value.AccessTokenExpiresAtUtc,
                    value.RefreshToken,
                    value.RefreshTokenExpiresAtUtc));
        }

        if (result.Error ==
            LoginErrors.InvalidCredentials)
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
                    StatusCodes.Status401Unauthorized);
        }

        if (result.Error ==
            LoginErrors.EmailNotConfirmed)
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
                    StatusCodes.Status403Forbidden);
        }

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
}
