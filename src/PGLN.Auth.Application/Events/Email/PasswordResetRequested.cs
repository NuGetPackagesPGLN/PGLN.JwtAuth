using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Events.Email;

public sealed record PasswordResetRequested(
    Guid EventId,
    UserId UserId,
    string Email,
    string ResetToken,
    DateTimeOffset OccurredAtUtc)
    : IIntegrationEvent;
