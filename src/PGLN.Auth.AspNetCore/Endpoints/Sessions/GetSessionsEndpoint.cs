using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Features.Sessions.GetSessions;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.AspNetCore.Endpoints.Sessions;

internal static class GetSessionsEndpoint
{
    public static IEndpointRouteBuilder MapGetSessionsEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet(
                "/sessions",
                HandleAsync)
            .RequireAuthorization()
            .WithName("GetAuthSessions");

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
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
                new GetSessionsQuery(
                    new UserId(userIdValue)),
                cancellationToken);

        if (result.IsFailure)
        {
            return Results.BadRequest(
                result.Error);
        }

        return Results.Ok(
            result.Value);
    }
}


