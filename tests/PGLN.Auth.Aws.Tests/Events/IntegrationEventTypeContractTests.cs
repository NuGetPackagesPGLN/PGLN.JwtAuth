using PGLN.Auth.Application.Events.Dispatching;
using PGLN.Auth.Application.Events.Email;

namespace PGLN.Auth.Aws.Tests.Events;

public sealed class IntegrationEventTypeContractTests
{
    [Fact]
    public void GetEventTypeName_ShouldReturnCanonicalFullName()
    {
        var eventTypeName =
            IntegrationEventTypeRegistry
                .GetEventTypeName(
                    typeof(EmailConfirmationRequested));

        Assert.Equal(
            typeof(EmailConfirmationRequested).FullName,
            eventTypeName);

        Assert.DoesNotContain(
            "Version=",
            eventTypeName,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "PublicKeyToken=",
            eventTypeName,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            ", PGLN.Auth.Application",
            eventTypeName,
            StringComparison.Ordinal);
    }

    [Fact]
    public void GetEventType_ShouldResolveCanonicalName()
    {
        var registry =
            new IntegrationEventTypeRegistry();

        var eventTypeName =
            IntegrationEventTypeRegistry
                .GetEventTypeName(
                    typeof(EmailConfirmationRequested));

        var resolvedType =
            registry.GetEventType(
                eventTypeName);

        Assert.Equal(
            typeof(EmailConfirmationRequested),
            resolvedType);
    }

    [Fact]
    public void GetEventType_ShouldRejectAssemblyQualifiedName()
    {
        var registry =
            new IntegrationEventTypeRegistry();

        var assemblyQualifiedName =
            typeof(EmailConfirmationRequested)
                .AssemblyQualifiedName!;

        Assert.Throws<InvalidOperationException>(
            () =>
                registry.GetEventType(
                    assemblyQualifiedName));
    }
}
