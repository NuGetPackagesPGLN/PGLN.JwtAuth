using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Tests.TestDoubles;

internal sealed class FakeAccessTokenGenerator
    : IAccessTokenGenerator
{
    public string Token { get; set; } =
        "fake-access-token";

    public TimeSpan Lifetime { get; set; } =
        TimeSpan.FromMinutes(15);

    public int GenerateCallCount { get; private set; }

    public User? LastUser { get; private set; }

    public DateTimeOffset? LastIssuedAtUtc { get; private set; }

    public AccessTokenResult Generate(
        User user,
        DateTimeOffset issuedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(
            user);

        GenerateCallCount++;

        LastUser =
            user;

        LastIssuedAtUtc =
            issuedAtUtc;

        return new AccessTokenResult(
            Token,
            issuedAtUtc.Add(
                Lifetime));
    }
}
