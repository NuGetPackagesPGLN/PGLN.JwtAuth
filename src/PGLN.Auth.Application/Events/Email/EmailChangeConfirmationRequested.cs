using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Events.Email;

public sealed record EmailChangeConfirmationRequested(
    Guid EventId,
    UserId UserId,
    string NewEmail,
    string VerificationToken,
    DateTimeOffset OccurredAtUtc)
    : IIntegrationEvent;
