using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Features.Sessions.TrustDevice;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.AspNetCore.Endpoints.TrustedDevices;

internal static class TrustCurrentDeviceEndpoint
{
    public static IEndpointRouteBuilder MapTrustCurrentDeviceEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapPost(
                "/trusted-devices/current",
                HandleAsync)
            .RequireAuthorization()
            .WithName("TrustCurrentDevice");

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

        var sessionClaim =
            principal.FindFirstValue(
                "sid");

        if (!Guid.TryParse(
                sessionClaim,
                out var sessionIdValue))
        {
            return Results.Unauthorized();
        }

        var result =
            await dispatcher.SendAsync(
                new TrustDeviceCommand(
                    new UserId(userIdValue),
                    new AuthSessionId(sessionIdValue)),
                cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "Sessions.NotFound")
            {
                return Results.NotFound(
                    result.Error);
            }

            if (result.Error.Code == "Sessions.Revoked")
            {
                return Results.Unauthorized();
            }

            return Results.BadRequest(
                result.Error);
        }

        return Results.NoContent();
    }
}
