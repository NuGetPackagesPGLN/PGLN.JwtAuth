using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Inbox;
using PGLN.Auth.EntityFrameworkCore;
using PGLN.Auth.EntityFrameworkCore.Inbox;

namespace PGLN.Auth.IntegrationTests.DependencyInjection;

public sealed class EntityFrameworkCoreDependencyInjectionTests
{
    [Fact]
    public void AddPGLNAuthEntityFrameworkCore_ShouldRegisterIntegrationEventInbox()
    {
        var services =
            new ServiceCollection();

        services.AddPGLNAuthEntityFrameworkCore(
            options =>
                options.UseSqlite(
                    "Data Source=:memory:"));

        using var serviceProvider =
            services.BuildServiceProvider();

        using var scope =
            serviceProvider.CreateScope();

        var inbox =
            scope.ServiceProvider
                .GetRequiredService<IIntegrationEventInbox>();

        Assert.IsType<EfIntegrationEventInbox>(
            inbox);
    }
}
