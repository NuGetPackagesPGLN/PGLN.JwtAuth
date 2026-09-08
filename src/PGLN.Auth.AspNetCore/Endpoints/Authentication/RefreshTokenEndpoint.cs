using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Features.TokenRefresh;
using PGLN.Auth.Contracts.Authentication;

namespace PGLN.Auth.AspNetCore.Endpoints.Authentication;

public static class RefreshTokenEndpoint
{
    public static RouteHandlerBuilder MapRefreshTokenEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/refresh",
            HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        RefreshTokenRequest request,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result =
            await dispatcher.SendAsync(
                new TokenRefreshCommand(
                    request.RefreshToken),
                cancellationToken);

        if (result.IsSuccess)
        {
            var value =
                result.Value;

            return Results.Ok(
                new RefreshTokenResponse(
                    value.UserId,
                    value.Email,
                    value.AccessToken,
                    value.AccessTokenExpiresAtUtc,
                    value.RefreshToken,
                    value.RefreshTokenExpiresAtUtc));
        }

        if (
            result.Error == TokenRefreshErrors.InvalidToken ||
            result.Error == TokenRefreshErrors.ExpiredToken ||
            result.Error == TokenRefreshErrors.RevokedToken ||
            result.Error == TokenRefreshErrors.UserNotFound)
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
