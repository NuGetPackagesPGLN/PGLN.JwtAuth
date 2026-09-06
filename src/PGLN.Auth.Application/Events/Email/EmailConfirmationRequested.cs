using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Events.Email;

public sealed record EmailConfirmationRequested(
    Guid EventId,
    UserId UserId,
    string Email,
    string VerificationToken,
    DateTimeOffset OccurredAtUtc)
    : IIntegrationEvent;
