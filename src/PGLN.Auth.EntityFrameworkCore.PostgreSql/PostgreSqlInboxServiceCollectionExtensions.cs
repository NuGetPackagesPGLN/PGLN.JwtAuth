using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.EntityFrameworkCore;

namespace PGLN.Auth.EntityFrameworkCore.PostgreSql;

public static class PostgreSqlInboxServiceCollectionExtensions
{
    public static IServiceCollection AddPGLNAuthPostgreSqlIntegrationEventInbox(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(
            services);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            connectionString);

        return services.AddPGLNAuthIntegrationEventInbox(
            options =>
            {
                options.UseNpgsql(
                    connectionString,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(
                            typeof(PostgreSqlAuthAssembly)
                                .Assembly
                                .GetName()
                                .Name);
                    });
            });
    }
}
