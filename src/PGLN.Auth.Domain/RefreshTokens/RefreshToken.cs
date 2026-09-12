using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Domain.RefreshTokens;

public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

    private RefreshToken(
        RefreshTokenId id,
        RefreshTokenFamilyId familyId,
        UserId userId,
        AuthSessionId sessionId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        Id =
            id;

        FamilyId =
            familyId;

        UserId =
            userId;

        SessionId =
            sessionId;

        TokenHash =
            tokenHash;

        CreatedAtUtc =
            createdAtUtc;

        ExpiresAtUtc =
            expiresAtUtc;
    }

    public RefreshTokenId Id { get; private set; }

    public RefreshTokenFamilyId FamilyId { get; private set; }

    public UserId UserId { get; private set; }

    public AuthSessionId SessionId { get; private set; }

    public string TokenHash { get; private set; } =
        string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public string? RevocationReason { get; private set; }

    public RefreshTokenId? ReplacedByTokenId { get; private set; }

    public bool IsRevoked =>
        RevokedAtUtc.HasValue;

    public bool IsExpired(
        DateTimeOffset now)
    {
        return now >= ExpiresAtUtc;
    }

    public bool IsActive(
        DateTimeOffset now)
    {
        return
            !IsRevoked &&
            !IsExpired(now);
    }

    public static RefreshToken Create(
        RefreshTokenId id,
        RefreshTokenFamilyId familyId,
        UserId userId,
        AuthSessionId sessionId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            tokenHash);

        if (familyId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Refresh token family id cannot be empty.",
                nameof(familyId));
        }

        if (expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentException(
                "Refresh token expiration time must be later than its creation time.",
                nameof(expiresAtUtc));
        }

        return new RefreshToken(
            id,
            familyId,
            userId,
            sessionId,
            tokenHash,
            createdAtUtc,
            expiresAtUtc);
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
        DateTimeOffset revokedAtUtc)
    {
        if (IsRevoked)
        {
            return;
        }

        if (revokedAtUtc < CreatedAtUtc)
        {
            throw new ArgumentException(
                "The rotation time cannot be earlier than the creation time.",
                nameof(revokedAtUtc));
        }

        RevokedAtUtc =
            revokedAtUtc;

        RevocationReason =
            "Rotated";

        ReplacedByTokenId =
            replacementTokenId;
    }
}

