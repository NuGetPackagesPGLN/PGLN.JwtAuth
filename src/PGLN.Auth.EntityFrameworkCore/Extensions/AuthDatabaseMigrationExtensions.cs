using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.EntityFrameworkCore.Extensions;

public static class AuthDatabaseMigrationExtensions
{
    public static async Task ApplyPGLNAuthMigrationsAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            serviceProvider);

        await using var scope =
            serviceProvider.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        await dbContext.Database.MigrateAsync(
            cancellationToken);
    }
}
