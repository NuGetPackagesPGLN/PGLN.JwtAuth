using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Email;

namespace PGLN.Auth.IntegrationTests.Facade;

public sealed class PGLNAuthEmailRegistrationTests
{
    [Fact]
    public void AddPGLNAuthEmail_RegistersConfiguredEmailSender()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPGLNAuthEmail<TestEmailSender>();

        using var serviceProvider =
            services.BuildServiceProvider();

        using var scope =
            serviceProvider.CreateScope();

        // Assert
        var emailSender =
            scope.ServiceProvider.GetRequiredService<IEmailSender>();

        Assert.IsType<TestEmailSender>(emailSender);
    }

    private sealed class TestEmailSender : IEmailSender
    {
        public Task SendAsync(
            EmailMessage message,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
