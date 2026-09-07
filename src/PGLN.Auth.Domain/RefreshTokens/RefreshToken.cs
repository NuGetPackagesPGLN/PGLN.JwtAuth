using PGLN.Auth.Domain.Common;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Domain.RefreshTokens;

public sealed class RefreshToken
    : Entity<RefreshTokenId>
{
    private RefreshToken()
        : base(default)
    {
        TokenHash = string.Empty;
    }

    private RefreshToken(
        RefreshTokenId id,
        UserId userId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public UserId UserId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public string? RevocationReason { get; private set; }

    public RefreshTokenId? ReplacedByTokenId { get; private set; }

    public bool IsRevoked =>
        RevokedAtUtc.HasValue;

    public static RefreshToken Create(
        RefreshTokenId id,
        UserId userId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            tokenHash);

        if (expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentException(
                "The expiration time must be later than the creation time.",
                nameof(expiresAtUtc));
        }

        return new RefreshToken(
            id,
            userId,
            tokenHash,
            createdAtUtc,
            expiresAtUtc);
    }

    public bool IsExpired(
        DateTimeOffset utcNow)
    {
        return utcNow >= ExpiresAtUtc;
    }

    public bool IsActive(
        DateTimeOffset utcNow)
    {
        return !IsRevoked &&
               !IsExpired(utcNow);
    }

    public void Revoke(
        DateTimeOffset revokedAtUtc,
        string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            reason);

        if (IsRevoked)
        {
            return;
        }

        if (revokedAtUtc < CreatedAtUtc)
        {
            throw new ArgumentException(
                "The revocation time cannot be earlier than the creation time.",
                nameof(revokedAtUtc));
        }

        RevokedAtUtc =
            revokedAtUtc;

        RevocationReason =
            reason;
    }

    public void Rotate(
        RefreshTokenId replacementTokenId,
        DateTimeOffset rotatedAtUtc)
    {
        if (IsRevoked)
        {
            return;
        }

        if (rotatedAtUtc < CreatedAtUtc)
        {
            throw new ArgumentException(
                "The rotation time cannot be earlier than the creation time.",
                nameof(rotatedAtUtc));
        }

        RevokedAtUtc =
            rotatedAtUtc;

        RevocationReason =
            "Rotated";

        ReplacedByTokenId =
            replacementTokenId;
    }
}
