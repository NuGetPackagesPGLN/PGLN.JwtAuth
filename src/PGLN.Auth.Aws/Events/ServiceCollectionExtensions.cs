using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Events;

namespace PGLN.Auth.Aws.Events;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPGLNAuthAwsSqs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var section =
            configuration.GetSection(
                SqsOptions.SectionName);

        if (!section.Exists())
        {
            throw new InvalidOperationException(
                $"AWS SQS configuration section '{SqsOptions.SectionName}' is missing.");
        }

        services
            .AddOptions<SqsOptions>()
            .Bind(section)
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(
                        options.Region),
                $"{SqsOptions.SectionName}:Region is required.")
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(
                        options.QueueUrl),
                $"{SqsOptions.SectionName}:QueueUrl is required.")
            .ValidateOnStart();

        var regionName =
            section[nameof(SqsOptions.Region)];

        if (string.IsNullOrWhiteSpace(regionName))
        {
            throw new InvalidOperationException(
                $"{SqsOptions.SectionName}:Region is required.");
        }

        var regionEndpoint =
            RegionEndpoint.GetBySystemName(
                regionName);

        var awsOptions =
            new AWSOptions
            {
                Region = regionEndpoint
            };

        services.AddDefaultAWSOptions(
            awsOptions);

        services.AddAWSService<IAmazonSQS>();

        services.AddScoped<
            IIntegrationEventDispatcher,
            SqsIntegrationEventDispatcher>();

        return services;
    }
}
