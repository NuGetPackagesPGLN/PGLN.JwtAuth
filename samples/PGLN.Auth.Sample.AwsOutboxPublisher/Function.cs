using Amazon.Lambda.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PGLN.Auth;
using PGLN.Auth.Aws.Events;
using PGLN.Auth.Aws.Events.Outbox;
using OutboxPublisher = PGLN.Auth.Aws.Events.Outbox.AwsOutboxPublisher;

[assembly: LambdaSerializer(
    typeof(
        Amazon.Lambda.Serialization.SystemTextJson
            .DefaultLambdaJsonSerializer))]

namespace PGLN.Auth.Sample.AwsOutboxPublisher;

public sealed class Function
{
    private readonly ServiceProvider _serviceProvider;

    public Function()
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

        services.AddPGLNAuthPostgreSql(
            configuration,
            connectionString);

        services.AddPGLNAuthAwsSqs(
            configuration);

        services.AddSingleton<OutboxPublisher>();

        _serviceProvider =
            services.BuildServiceProvider(
                validateScopes: true);
    }

    public async Task<int> FunctionHandler(
        object? input,
        ILambdaContext context)
    {
        ArgumentNullException.ThrowIfNull(
            context);

        var publisher =
            _serviceProvider
                .GetRequiredService<OutboxPublisher>();

        var workerId =
            $"aws-lambda:{context.AwsRequestId}";

        context.Logger.LogInformation(
            "PGLN.Auth outbox publisher {WorkerId} started.",
            workerId);

        var processed =
            await publisher.PublishAsync(
                workerId);

        context.Logger.LogInformation(
            "PGLN.Auth outbox publisher {WorkerId} processed {MessageCount} message(s).",
            workerId,
            processed);

        return processed;
    }
}
