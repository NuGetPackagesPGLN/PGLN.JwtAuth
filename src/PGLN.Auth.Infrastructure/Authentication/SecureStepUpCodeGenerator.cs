using System.Security.Cryptography;
using PGLN.Auth.Application.Abstractions.Authentication;

namespace PGLN.Auth.Infrastructure.Authentication;

public sealed class SecureStepUpCodeGenerator
    : IStepUpCodeGenerator
{
    public string Generate()
    {
        var value =
            RandomNumberGenerator.GetInt32(
                0,
                1_000_000);

        return value.ToString("D6");
    }
}
