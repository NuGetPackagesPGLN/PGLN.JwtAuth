using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Domain.PasswordResets;

public sealed class PasswordResetToken
{
    private PasswordResetToken()
    {
    }

    private PasswordResetToken(
        PasswordResetTokenId id,
        UserId userId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        Id =
            id;

        UserId =
            userId;

        TokenHash =
            tokenHash;

        CreatedAtUtc =
            createdAtUtc;

        ExpiresAtUtc =
            expiresAtUtc;
    }

    public PasswordResetTokenId Id { get; private set; }

    public UserId UserId { get; private set; }

    public string TokenHash { get; private set; } =
        string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? UsedAtUtc { get; private set; }

    public bool IsUsed =>
        UsedAtUtc.HasValue;

    public bool IsExpired(
        DateTimeOffset now)
    {
        return now >= ExpiresAtUtc;
    }

    public bool CanBeUsed(
        DateTimeOffset now)
    {
        return
            !IsUsed &&
            !IsExpired(now);
    }

    public static PasswordResetToken Create(
        PasswordResetTokenId id,
        UserId userId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Password reset token id cannot be empty.",
                nameof(id));
        }

        if (userId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "User id cannot be empty.",
                nameof(userId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            tokenHash);

        if (expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentException(
                "Password reset token expiration must be later than its creation time.",
                nameof(expiresAtUtc));
        }

        return new PasswordResetToken(
            id,
            userId,
            tokenHash,
            createdAtUtc,
            expiresAtUtc);
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

        if (usedAtUtc >= ExpiresAtUtc)
        {
            throw new InvalidOperationException(
                "An expired password reset token cannot be used.");
        }

        UsedAtUtc =
            usedAtUtc;
    }
}
