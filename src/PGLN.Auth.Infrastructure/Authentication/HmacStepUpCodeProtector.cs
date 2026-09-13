using System.Security.Cryptography;
using System.Text;
using PGLN.Auth.Application.Abstractions.Authentication;

namespace PGLN.Auth.Infrastructure.Authentication;

public sealed class HmacStepUpCodeProtector
    : IStepUpCodeProtector
{
    private readonly byte[] _key;

    public HmacStepUpCodeProtector(
        string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            secret);

        _key =
            Encoding.UTF8.GetBytes(
                secret);
    }

    public string Protect(
        string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            code);

        using var hmac =
            new HMACSHA256(
                _key);

        var bytes =
            Encoding.UTF8.GetBytes(
                code);

        var hash =
            hmac.ComputeHash(
                bytes);

        return Convert.ToHexString(
            hash);
    }

    public bool Verify(
        string code,
        string protectedCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            code);

        if (string.IsNullOrWhiteSpace(
            protectedCode))
        {
            return false;
        }

        try
        {
            var actual =
                Protect(
                    code);

            var actualBytes =
                Convert.FromHexString(
                    actual);

            var expectedBytes =
                Convert.FromHexString(
                    protectedCode);

            if (actualBytes.Length !=
                expectedBytes.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(
                actualBytes,
                expectedBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
