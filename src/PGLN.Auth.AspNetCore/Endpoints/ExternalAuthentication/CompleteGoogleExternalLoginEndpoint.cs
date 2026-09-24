using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Features.ExternalAuthentication;
using PGLN.Auth.Application.Features.ExternalAuthentication.CompleteExternalLogin;
using PGLN.Auth.Domain.ExternalLogins;

namespace PGLN.Auth.AspNetCore.Endpoints.ExternalAuthentication;

internal static class CompleteGoogleExternalLoginEndpoint
{
    public static RouteHandlerBuilder MapCompleteGoogleExternalLoginEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet(
            "/external/google/callback",
            HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        string? code,
        string? error,
        string state,
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
                    httpContext.Connection.RemoteIpAddress?.ToString(),
                    httpContext.Request.Headers.UserAgent.ToString(),
                    error),
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
            ExternalAuthenticationErrors.IdentityInvalid ||
            result.Error ==
            ExternalAuthenticationErrors.AuthorizationDenied)
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

        if (result.Error ==
            ExternalAuthenticationErrors.ProviderFailure)
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
                    StatusCodes.Status502BadGateway);
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
