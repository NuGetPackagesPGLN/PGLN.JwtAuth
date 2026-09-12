using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Features.TrustedDevices.RevokeTrustedDevice;
using PGLN.Auth.Domain.TrustedDevices;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.AspNetCore.Endpoints.TrustedDevices;

public static class RevokeTrustedDeviceEndpoint
{
    public static IEndpointRouteBuilder MapRevokeTrustedDeviceEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapDelete(
                "/trusted-devices/{trustedDeviceId:guid}",
                HandleAsync)
            .RequireAuthorization()
            .WithName("RevokeTrustedDevice");

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        Guid trustedDeviceId,
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
                new RevokeTrustedDeviceCommand(
                    new UserId(userIdValue),
                    new TrustedDeviceId(trustedDeviceId)),
                cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "TrustedDevices.NotFound")
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
