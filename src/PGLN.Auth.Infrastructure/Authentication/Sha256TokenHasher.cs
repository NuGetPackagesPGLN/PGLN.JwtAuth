using System.Security.Cryptography;
using System.Text;
using PGLN.Auth.Application.Abstractions.Authentication;

namespace PGLN.Auth.Infrastructure.Authentication;

public sealed class Sha256TokenHasher
    : ITokenHasher
{
    public string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var bytes =
            Encoding.UTF8.GetBytes(token);

        var hash =
            SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }
}
