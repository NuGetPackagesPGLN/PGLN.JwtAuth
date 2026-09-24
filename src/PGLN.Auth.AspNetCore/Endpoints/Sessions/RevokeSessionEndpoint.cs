using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Features.Sessions.RevokeSession;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.AspNetCore.Endpoints.Sessions;

internal static class RevokeSessionEndpoint
{
    public static IEndpointRouteBuilder MapRevokeSessionEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapDelete(
                "/sessions/{sessionId:guid}",
                HandleAsync)
            .RequireAuthorization()
            .WithName("RevokeAuthSession");

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        Guid sessionId,
        ClaimsPrincipal principal,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var subject =
            principal.FindFirstValue(
                "sub");

        if (!Guid.TryParse(
                subject,
                out var userIdValue))
        {
            return Results.Unauthorized();
        }

        var result =
            await dispatcher.SendAsync(
                new RevokeSessionCommand(
                    new UserId(userIdValue),
                    new AuthSessionId(sessionId)),
                cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "Sessions.NotFound")
            {
                return Results.NotFound(
                    result.Error);
            }

            return Results.BadRequest(
                result.Error);
        }

        return Results.NoContent();
    }
}
