using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.IntegrationTests.TestHelpers;

internal static class AuthSessionTestFactory
{
    public static AuthSession Create(
        UserId userId,
        DateTimeOffset createdAtUtc,
        string? deviceIdHash = null)
    {
        return AuthSession.Create(
            AuthSessionId.New(),
            userId,
            deviceIdHash ?? $"test-device-{Guid.NewGuid():N}",
            "Integration Test Device",
            "127.0.0.1",
            "PGLN.Auth.IntegrationTests",
            createdAtUtc);
    }
}
