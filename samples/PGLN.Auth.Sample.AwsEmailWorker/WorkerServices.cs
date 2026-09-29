using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Email;
using PGLN.Auth.Application.Events;
using PGLN.Auth.Aws.Email;
using PGLN.Auth.EntityFrameworkCore.PostgreSql;
using PGLN.Auth.Infrastructure.Email;

namespace PGLN.Auth.Sample.AwsEmailWorker;

internal static class WorkerServices
{
    private static readonly Lazy<IConfiguration> LazyConfiguration =
        new(CreateConfiguration);

    private static readonly Lazy<IServiceProvider> LazyProvider =
        new(CreateProvider);

    public static IConfiguration Configuration =>
        LazyConfiguration.Value;

    public static IServiceProvider Provider =>
        LazyProvider.Value;

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddUserSecrets<Function>(
                optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static IServiceProvider CreateProvider()
    {
        var configuration =
            Configuration;

        var services =
            new ServiceCollection();

        services.AddSingleton<IConfiguration>(
            configuration);

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
            .AddPGLNAuthPostgreSqlIntegrationEventInbox(
                connectionString);

        services.AddPGLNAuthIntegrationEvents();

        var emailDeliveryOptions =
            new EmailDeliveryOptions();

        emailDeliveryOptions.Validate();

        services.AddSingleton(
            emailDeliveryOptions);

        services.AddPGLNAuthDefaultEmailTemplates();

        services.AddPGLNAuthAwsSes(
            configuration);

        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateScopes = true,
                ValidateOnBuild = true
            });
    }
}
