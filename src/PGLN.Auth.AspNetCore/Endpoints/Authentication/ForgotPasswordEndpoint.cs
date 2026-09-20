using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common.Validation;
using PGLN.Auth.Application.Features.ForgotPassword;
using PGLN.Auth.Contracts.Authentication;

namespace PGLN.Auth.AspNetCore.Endpoints.Authentication;

internal static class ForgotPasswordEndpoint
{
    public static RouteHandlerBuilder MapForgotPassword(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(
            endpoints);

        return endpoints.MapPost(
            "/forgot-password",
            HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        ForgotPasswordRequest request,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        try
        {
            await dispatcher.SendAsync(
                new ForgotPasswordCommand(
                    request.Email),
                cancellationToken);

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
