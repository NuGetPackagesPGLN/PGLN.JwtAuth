using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Features.TrustedDevices.GetTrustedDevices;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.AspNetCore.Endpoints.TrustedDevices;

internal static class GetTrustedDevicesEndpoint
{
    public static IEndpointRouteBuilder MapGetTrustedDevicesEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet(
                "/trusted-devices",
                HandleAsync)
            .RequireAuthorization()
            .WithName("GetTrustedDevices");

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
            await dispatcher.QueryAsync(
                new GetTrustedDevicesQuery(
                    new UserId(userIdValue)),
                cancellationToken);

        return Results.Ok(
            result);
    }
}

