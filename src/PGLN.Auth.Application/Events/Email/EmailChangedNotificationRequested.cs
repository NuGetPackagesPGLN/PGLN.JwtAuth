using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Events.Email;

public sealed record EmailChangedNotificationRequested(
    Guid EventId,
    UserId UserId,
    string OldEmail,
    string NewEmail,
    DateTimeOffset OccurredAtUtc)
    : IIntegrationEvent;
