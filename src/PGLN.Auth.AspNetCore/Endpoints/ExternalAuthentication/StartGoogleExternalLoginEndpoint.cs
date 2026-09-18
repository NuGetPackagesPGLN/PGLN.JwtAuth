using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Features.ExternalAuthentication;
using PGLN.Auth.Application.Features.ExternalAuthentication.StartExternalLogin;
using PGLN.Auth.Domain.ExternalLogins;

namespace PGLN.Auth.AspNetCore.Endpoints.ExternalAuthentication;

public static class StartGoogleExternalLoginEndpoint
{
    public static RouteHandlerBuilder MapStartGoogleExternalLoginEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet(
            "/external/google/start",
            HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        string redirectUri,
        string deviceIdHash,
        string? deviceName,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result =
            await dispatcher.SendAsync(
                new StartExternalLoginCommand(
                    ExternalLoginProvider.Google,
                    redirectUri,
                    deviceIdHash,
                    deviceName),
                cancellationToken);

        if (result.IsSuccess)
        {
            return Results.Redirect(
                result.Value.AuthorizationUrl);
        }

        if (result.Error ==
            ExternalAuthenticationErrors.ProviderNotSupported)
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
