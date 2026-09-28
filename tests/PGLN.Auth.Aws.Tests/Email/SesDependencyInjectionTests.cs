using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PGLN.Auth.Application.Abstractions.Email;
using PGLN.Auth.Aws.Email;

namespace PGLN.Auth.Aws.Tests.Email;

public sealed class SesDependencyInjectionTests
{
    [Fact]
    public void AddPGLNAuthAwsSes_MissingSection_ShouldThrow()
    {
        // Arrange
        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>())
                .Build();

        var services =
            new ServiceCollection();

        // Act
        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    services.AddPGLNAuthAwsSes(
                        configuration));

        // Assert
        Assert.Contains(
            SesOptions.SectionName,
            exception.Message);
    }

    [Fact]
    public void AddPGLNAuthAwsSes_MissingRegion_ShouldThrow()
    {
        // Arrange
        var configuration =
            CreateConfiguration(
                region: null,
                fromAddress: "auth@example.com");

        var services =
            new ServiceCollection();

        // Act
        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    services.AddPGLNAuthAwsSes(
                        configuration));

        // Assert
        Assert.Contains(
            "Region is required",
            exception.Message);
    }

    [Fact]
    public void AddPGLNAuthAwsSes_ValidConfiguration_ShouldBindOptions()
    {
        // Arrange
        var configuration =
            CreateConfiguration(
                region: "af-south-1",
                fromAddress: "auth@example.com");

        var services =
            new ServiceCollection();

        services.AddPGLNAuthAwsSes(
            configuration);

        using var provider =
            services.BuildServiceProvider();

        // Act
        var options =
            provider
                .GetRequiredService<IOptions<SesOptions>>()
                .Value;

        // Assert
        Assert.Equal(
            "af-south-1",
            options.Region);

        Assert.Equal(
            "auth@example.com",
            options.FromAddress);
    }

    [Fact]
    public void AddPGLNAuthAwsSes_ValidConfiguration_ShouldRegisterEmailSender()
    {
        // Arrange
        var configuration =
            CreateConfiguration(
                region: "af-south-1",
                fromAddress: "auth@example.com");

        var services =
            new ServiceCollection();

        services.AddPGLNAuthAwsSes(
            configuration);

        using var provider =
            services.BuildServiceProvider();

        // Act
        var sender =
            provider.GetRequiredService<IEmailSender>();

        // Assert
        Assert.IsType<SesEmailSender>(
            sender);
    }

    private static IConfiguration CreateConfiguration(
        string? region,
        string? fromAddress)
    {
        var values =
            new Dictionary<string, string?>();

        if (region is not null)
        {
            values[
                $"{SesOptions.SectionName}:Region"] =
                region;
        }

        if (fromAddress is not null)
        {
            values[
                $"{SesOptions.SectionName}:FromAddress"] =
                fromAddress;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
