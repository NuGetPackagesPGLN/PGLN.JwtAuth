using PGLN.Auth.EntityFrameworkCore.Extensions;

namespace PGLN.Auth;

public static class PGLNAuthDatabaseExtensions
{
    public static Task ApplyPGLNAuthMigrationsAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            serviceProvider);

        return AuthDatabaseMigrationExtensions
            .ApplyPGLNAuthMigrationsAsync(
                serviceProvider,
                cancellationToken);
    }
}
