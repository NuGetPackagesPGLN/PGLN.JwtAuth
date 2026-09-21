using Microsoft.AspNetCore.Builder;
using PGLN.Auth.AspNetCore.Middleware;

namespace PGLN.Auth.AspNetCore.Extensions;

public static class AuthApplicationBuilderExtensions
{
    public static IApplicationBuilder UsePGLNAuth(
        this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(
            app);

        return app.UseMiddleware<
            CommandValidationExceptionMiddleware>();
    }
}
