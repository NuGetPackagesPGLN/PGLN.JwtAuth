using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Domain.StepUpChallenges;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Events.Email;

public sealed record StepUpVerificationCodeRequested(
    Guid EventId,
    UserId UserId,
    StepUpChallengeId ChallengeId,
    string Email,
    string Code,
    string? DeviceName,
    string? IpAddress,
    string? UserAgent,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset OccurredAtUtc)
    : IIntegrationEvent;
