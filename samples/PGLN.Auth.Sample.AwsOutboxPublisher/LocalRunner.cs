using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PGLN.Auth;
using PGLN.Auth.Aws.Events;
using PGLN.Auth.Aws.Events.Outbox;
using OutboxPublisher =
    PGLN.Auth.Aws.Events.Outbox.AwsOutboxPublisher;

namespace PGLN.Auth.Sample.AwsOutboxPublisher;

public static class LocalRunner
{
    public static async Task<int> RunAsync()
    {
        var configuration =
            new ConfigurationBuilder()
                .AddUserSecrets<Function>(
                    optional: true)
                .AddEnvironmentVariables()
                .Build();

        var services =
            new ServiceCollection();

        services.AddLogging(
            builder =>
            {
                builder.AddConsole();
            });

        var connectionString =
            configuration.GetConnectionString(
                "PGLNAuth");

        if (string.IsNullOrWhiteSpace(
                connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'PGLNAuth' is required.");
        }

        services
            .AddDataProtection()
            .SetApplicationName("PGLN.Auth.Aws");

        services.AddPGLNAuthPostgreSql(
            configuration,
            connectionString);

        services.AddPGLNAuthAwsSqs(
            configuration);

        services.AddSingleton<OutboxPublisher>();

        await using var serviceProvider =
            services.BuildServiceProvider(
                validateScopes: true);

        var publisher =
            serviceProvider
                .GetRequiredService<OutboxPublisher>();

        var workerId =
            $"local:{Guid.NewGuid():N}";

        Console.WriteLine(
            $"Local PGLN.Auth outbox publisher {workerId} started.");

        var processed =
            await publisher.PublishAsync(
                workerId);

        Console.WriteLine(
            $"Local PGLN.Auth outbox publisher processed {processed} message(s).");

        return processed;
    }
}
