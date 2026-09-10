using PGLN.Auth.Application.Abstractions.Security;

namespace PGLN.Auth.Application.Tests.TestDoubles;

internal sealed class FakePasswordResetTokenGenerator
    : IPasswordResetTokenGenerator
{
    public string Token { get; set; } =
        "raw-password-reset-token";

    public int GenerateCallCount { get; private set; }

    public string Generate()
    {
        GenerateCallCount++;

        return Token;
    }
}
