using PGLN.Auth.Application.Abstractions.Authentication;

namespace PGLN.Auth.Application.Tests.TestDoubles;

public sealed class FakeStepUpCodeProtector
    : IStepUpCodeProtector
{
    public int ProtectCallCount { get; private set; }

    public int VerifyCallCount { get; private set; }

    public string? LastProtectedCode { get; private set; }

    public string Protect(
        string code)
    {
        ProtectCallCount++;
        LastProtectedCode =
            code;

        return $"protected::{code}";
    }

    public bool Verify(
        string code,
        string protectedCode)
    {
        VerifyCallCount++;

        return protectedCode ==
            $"protected::{code}";
    }
}
