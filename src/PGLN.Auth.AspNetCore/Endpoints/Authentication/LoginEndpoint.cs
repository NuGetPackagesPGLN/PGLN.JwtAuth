using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Features.Login;
using PGLN.Auth.AspNetCore.RateLimiting;
using PGLN.Auth.Contracts.Authentication;

namespace PGLN.Auth.AspNetCore.Endpoints.Authentication;

public static class LoginEndpoint
{
    public static RouteHandlerBuilder MapLoginEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost(
                "/login",
                HandleAsync)
            .RequireRateLimiting(
                LoginRateLimitOptions.PolicyName);
    }

    private static async Task<IResult> HandleAsync(
        LoginRequest request,
        HttpContext httpContext,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result =
            await dispatcher.SendAsync(
                new LoginCommand(
                    request.Email,
                    request.Password,
                    request.DeviceIdHash,
                    request.DeviceName,
                    httpContext.Connection.RemoteIpAddress?.ToString(),
                    httpContext.Request.Headers.UserAgent.ToString()),
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
            LoginErrors.InvalidCredentials ||
            result.Error ==
            LoginErrors.AccountLocked)
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

        if (result.Error ==
            LoginErrors.TooManyAttempts)
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
                    StatusCodes.Status429TooManyRequests);
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
