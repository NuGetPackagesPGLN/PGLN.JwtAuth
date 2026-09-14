using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Features.ExternalAuthentication;
using PGLN.Auth.Application.Features.ExternalAuthentication.CompleteExternalLogin;
using PGLN.Auth.Domain.ExternalLogins;

namespace PGLN.Auth.AspNetCore.Endpoints.ExternalAuthentication;

public static class CompleteGoogleExternalLoginEndpoint
{
    public static RouteHandlerBuilder MapCompleteGoogleExternalLoginEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet(
            "/external/google/callback",
            HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        string code,
        string state,
        string redirectUri,
        string deviceIdHash,
        string? deviceName,
        HttpContext httpContext,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result =
            await dispatcher.SendAsync(
                new CompleteExternalLoginCommand(
                    ExternalLoginProvider.Google,
                    code,
                    state,
                    redirectUri,
                    deviceIdHash,
                    deviceName,
                    httpContext.Connection.RemoteIpAddress?.ToString(),
                    httpContext.Request.Headers.UserAgent.ToString()),
                cancellationToken);

        if (result.IsSuccess)
        {
            var value =
                result.Value;

            return Results.Ok(
                new
                {
                    userId =
                        value.UserId,

                    email =
                        value.Email,

                    accessToken =
                        value.AccessToken,

                    accessTokenExpiresAtUtc =
                        value.AccessTokenExpiresAtUtc,

                    refreshToken =
                        value.RefreshToken,

                    refreshTokenExpiresAtUtc =
                        value.RefreshTokenExpiresAtUtc,

                    isNewUser =
                        value.IsNewUser
                });
        }

        if (result.Error ==
            ExternalAuthenticationErrors.InvalidState ||
            result.Error ==
            ExternalAuthenticationErrors.IdentityInvalid)
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
            ExternalAuthenticationErrors.AccountLinkRequired)
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
                    StatusCodes.Status409Conflict);
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
