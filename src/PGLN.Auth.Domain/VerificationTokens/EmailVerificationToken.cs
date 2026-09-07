using PGLN.Auth.Domain.Common;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Domain.VerificationTokens;

public sealed class EmailVerificationToken
    : Entity<EmailVerificationTokenId>
{
    private EmailVerificationToken()
        : base(default)
    {
        TokenHash = string.Empty;
    }

    private EmailVerificationToken(
        EmailVerificationTokenId id,
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

    public DateTimeOffset? UsedAtUtc { get; private set; }

    public bool IsUsed =>
        UsedAtUtc.HasValue;

    public static EmailVerificationToken Create(
        EmailVerificationTokenId id,
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

        return new EmailVerificationToken(
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

    public bool CanBeUsed(
        DateTimeOffset utcNow)
    {
        return !IsUsed &&
               !IsExpired(utcNow);
    }

    public void Revoke(
        DateTimeOffset revokedAtUtc)
    {
        if (IsUsed)
        {
            return;
        }

        if (revokedAtUtc < CreatedAtUtc)
        {
            throw new ArgumentException(
                "The revocation time cannot be earlier than the creation time.",
                nameof(revokedAtUtc));
        }

        UsedAtUtc = revokedAtUtc;
    }

    public void MarkAsUsed(
        DateTimeOffset usedAtUtc)
    {
        if (IsUsed)
        {
            return;
        }

        if (usedAtUtc < CreatedAtUtc)
        {
            throw new ArgumentException(
                "The usage time cannot be earlier than the creation time.",
                nameof(usedAtUtc));
        }

        UsedAtUtc = usedAtUtc;
    }
}

