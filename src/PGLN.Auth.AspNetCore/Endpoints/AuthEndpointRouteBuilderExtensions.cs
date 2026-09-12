using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PGLN.Auth.AspNetCore.Endpoints.Authentication;
using PGLN.Auth.AspNetCore.Endpoints.EmailConfirmation;
using PGLN.Auth.AspNetCore.Endpoints.Registration;
using PGLN.Auth.AspNetCore.Endpoints.Sessions;

namespace PGLN.Auth.AspNetCore.Endpoints;

public static class AuthEndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapPGLNAuthEndpoints(
        this IEndpointRouteBuilder endpoints,
        string prefix = "/api/auth")
    {
        ArgumentNullException.ThrowIfNull(
            endpoints);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            prefix);

        var group =
            endpoints.MapGroup(
                prefix);

        RegisterEndpoint.Map(
            group);

        ConfirmEmailEndpoint.Map(
            group);

        ResendEmailConfirmationEndpoint.Map(
            group);

        group.MapLoginEndpoint();

        group.MapRefreshTokenEndpoint();

        group.MapLogout();

        group.MapLogoutAll();

        group.MapResetPassword();

        group.MapChangePassword()
            .RequireAuthorization();

        group.MapGetSessionsEndpoint();

        group.MapRevokeSessionEndpoint();

        group.MapRevokeOtherSessionsEndpoint();

        return group;
    }
}
