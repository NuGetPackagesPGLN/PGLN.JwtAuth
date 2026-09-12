using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Features.Sessions.RevokeOtherSessions;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.AspNetCore.Endpoints.Sessions;

internal static class RevokeOtherSessionsEndpoint
{
    public static RouteGroupBuilder MapRevokeOtherSessionsEndpoint(
        this RouteGroupBuilder group)
    {
        group.MapDelete(
                "/sessions/others",
                HandleAsync)
            .RequireAuthorization();

        return group;
    }

    private static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var userIdValue =
            httpContext.User.FindFirstValue(
                ClaimTypes.NameIdentifier)
            ??
            httpContext.User.FindFirstValue(
                "sub");

        if (!Guid.TryParse(
                userIdValue,
                out var userId))
        {
            return Results.Unauthorized();
        }

        var sessionIdValue =
            httpContext.User.FindFirstValue(
                "sid");

        if (!Guid.TryParse(
                sessionIdValue,
                out var sessionId))
        {
            return Results.Unauthorized();
        }

        var result =
            await dispatcher.SendAsync(
                new RevokeOtherSessionsCommand(
                    new UserId(userId),
                    new AuthSessionId(sessionId)),
                cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "Sessions.CurrentSessionNotFound" =>
                    Results.Unauthorized(),

                _ =>
                    Results.BadRequest(
                        result.Error)
            };
        }

        return Results.NoContent();
    }
}
