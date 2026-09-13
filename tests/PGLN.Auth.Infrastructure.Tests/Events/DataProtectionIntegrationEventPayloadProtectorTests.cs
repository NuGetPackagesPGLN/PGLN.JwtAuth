using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Infrastructure.Events;

namespace PGLN.Auth.Infrastructure.Tests.Events;

public sealed class DataProtectionIntegrationEventPayloadProtectorTests
{
    [Fact]
    public void Protect_ShouldNotReturnPlaintext()
    {
        var protector =
            CreateProtector();

        const string plaintext =
            """{"code":"123456","email":"user@example.com"}""";

        var protectedPayload =
            protector.Protect(
                plaintext);

        Assert.NotEqual(
            plaintext,
            protectedPayload);

        Assert.DoesNotContain(
            "123456",
            protectedPayload);

        Assert.DoesNotContain(
            "user@example.com",
            protectedPayload);
    }

    [Fact]
    public void ProtectAndUnprotect_ShouldRoundTrip()
    {
        var protector =
            CreateProtector();

        const string plaintext =
            """{"code":"123456","email":"user@example.com"}""";

        var protectedPayload =
            protector.Protect(
                plaintext);

        var unprotected =
            protector.Unprotect(
                protectedPayload);

        Assert.Equal(
            plaintext,
            unprotected);
    }

    [Fact]
    public void Unprotect_WhenPayloadIsTampered_ShouldThrow()
    {
        var protector =
            CreateProtector();

        const string plaintext =
            """{"code":"123456","email":"user@example.com"}""";

        var protectedPayload =
            protector.Protect(
                plaintext);

        var characters =
            protectedPayload.ToCharArray();

        var index =
            characters.Length / 2;

        characters[index] =
            characters[index] == 'A'
                ? 'B'
                : 'A';

        var tamperedPayload =
            new string(
                characters);

        Assert.ThrowsAny<Exception>(
            () =>
                protector.Unprotect(
                    tamperedPayload));
    }

    private static DataProtectionIntegrationEventPayloadProtector
        CreateProtector()
    {
        var services =
            new ServiceCollection();

        services.AddDataProtection();

        using var serviceProvider =
            services.BuildServiceProvider();

        var dataProtectionProvider =
            serviceProvider
                .GetRequiredService<IDataProtectionProvider>();

        return new DataProtectionIntegrationEventPayloadProtector(
            dataProtectionProvider);
    }
}
