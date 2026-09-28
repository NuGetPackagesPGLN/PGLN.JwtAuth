using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SimpleEmailV2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PGLN.Auth.Application.Abstractions.Email;

namespace PGLN.Auth.Aws.Email;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPGLNAuthAwsSes(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(SesOptions.SectionName);

        if (!section.Exists())
        {
            throw new InvalidOperationException(
                $"AWS SES configuration section '{SesOptions.SectionName}' is missing.");
        }

        services
            .AddOptions<SesOptions>()
            .Bind(section)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Region),
                $"{SesOptions.SectionName}:Region is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.FromAddress),
                $"{SesOptions.SectionName}:FromAddress is required.")
            .ValidateOnStart();

        var regionName = section[nameof(SesOptions.Region)];

        if (string.IsNullOrWhiteSpace(regionName))
        {
            throw new InvalidOperationException(
                $"{SesOptions.SectionName}:Region is required.");
        }

        var regionEndpoint = RegionEndpoint.GetBySystemName(regionName);

        var awsOptions = new AWSOptions
        {
            Region = regionEndpoint
        };

        services.AddDefaultAWSOptions(awsOptions);

        services.AddAWSService<IAmazonSimpleEmailServiceV2>();

        services.AddTransient<IEmailSender>(serviceProvider =>
        {
            var ses = serviceProvider
                .GetRequiredService<IAmazonSimpleEmailServiceV2>();

            var options = serviceProvider
                .GetRequiredService<IOptions<SesOptions>>()
                .Value;

            return new SesEmailSender(
                ses,
                options.FromAddress);
        });

        return services;
    }
}
