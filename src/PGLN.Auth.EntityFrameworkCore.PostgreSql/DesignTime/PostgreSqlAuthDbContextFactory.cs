using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.EntityFrameworkCore.PostgreSql.DesignTime;

public sealed class PostgreSqlAuthDbContextFactory
    : IDesignTimeDbContextFactory<AuthDbContext>
{
    private const string ConnectionStringEnvironmentVariable =
        "PGLN_AUTH_DESIGN_CONNECTION_STRING";

    public AuthDbContext CreateDbContext(
        string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                ConnectionStringEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(
                connectionString))
        {
            throw new InvalidOperationException(
                $"The environment variable '{ConnectionStringEnvironmentVariable}' " +
                "must be configured when creating PostgreSQL migrations.");
        }

        var optionsBuilder =
            new DbContextOptionsBuilder<AuthDbContext>();

        optionsBuilder.UseNpgsql(
            connectionString,
            npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(
                    typeof(PostgreSqlAuthDbContextFactory)
                        .Assembly
                        .GetName()
                        .Name);
            });

        return new AuthDbContext(
            optionsBuilder.Options);
    }
}
