using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Domain.StepUpChallenges;

public sealed class StepUpChallenge
{
    private StepUpChallenge()
    {
    }

    private StepUpChallenge(
        StepUpChallengeId id,
        UserId userId,
        string deviceIdHash,
        string? deviceName,
        string codeHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        Id = id;
        UserId = userId;
        DeviceIdHash = deviceIdHash;
        DeviceName = deviceName;
        CodeHash = codeHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public StepUpChallengeId Id { get; private set; }

    public UserId UserId { get; private set; }

    public string DeviceIdHash { get; private set; } =
        string.Empty;

    public string? DeviceName { get; private set; }

    public string CodeHash { get; private set; } =
        string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? VerifiedAtUtc { get; private set; }

    public int FailedAttempts { get; private set; }

    public bool IsVerified =>
        VerifiedAtUtc.HasValue;

    public bool IsExpired(
        DateTimeOffset now)
    {
        return now >= ExpiresAtUtc;
    }

    public static StepUpChallenge Create(
        StepUpChallengeId id,
        UserId userId,
        string deviceIdHash,
        string? deviceName,
        string codeHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            deviceIdHash);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            codeHash);

        if (expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentException(
                "The challenge expiry time must be later than the creation time.",
                nameof(expiresAtUtc));
        }

        return new StepUpChallenge(
            id,
            userId,
            deviceIdHash,
            deviceName,
            codeHash,
            createdAtUtc,
            expiresAtUtc);
    }

    public void RecordFailedAttempt()
    {
        if (IsVerified)
        {
            return;
        }

        FailedAttempts++;
    }

    public void MarkVerified(
        DateTimeOffset verifiedAtUtc)
    {
        if (IsVerified)
        {
            return;
        }

        if (verifiedAtUtc < CreatedAtUtc)
        {
            throw new ArgumentException(
                "The verification time cannot be earlier than the challenge creation time.",
                nameof(verifiedAtUtc));
        }

        if (verifiedAtUtc >= ExpiresAtUtc)
        {
            throw new InvalidOperationException(
                "An expired step-up challenge cannot be verified.");
        }

        VerifiedAtUtc =
            verifiedAtUtc;
    }
}
