using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Email;
using PGLN.Auth.Application.Events;
using PGLN.Auth.Aws.Email;
using PGLN.Auth.Infrastructure.Email;

namespace PGLN.Auth.Sample.AwsEmailWorker;

internal static class WorkerServices
{
    private static readonly Lazy<IServiceProvider> LazyProvider =
        new(CreateProvider);

    public static IServiceProvider Provider =>
        LazyProvider.Value;

    private static IServiceProvider CreateProvider()
    {
        var configuration =
            new ConfigurationBuilder()
                .AddUserSecrets<Function>()
                .AddEnvironmentVariables()
                .Build();

        var services =
            new ServiceCollection();

        services.AddSingleton<IConfiguration>(
            configuration);

        // Register only integration-event handlers + dispatcher.
        // Do not register the complete authentication application layer.
        services.AddPGLNAuthIntegrationEvents();

        // Some email handlers need these URLs when rendering links.
        var emailDeliveryOptions =
            new EmailDeliveryOptions();

        emailDeliveryOptions.Validate();

        services.AddSingleton(
            emailDeliveryOptions);

        // Default PGLN.Auth email templates.
        services.AddPGLNAuthDefaultEmailTemplates();

        // AWS SES implementation of IEmailSender.
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
