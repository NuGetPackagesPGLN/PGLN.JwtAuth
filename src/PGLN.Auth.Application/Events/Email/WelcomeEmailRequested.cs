using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Events.Email;

public sealed record WelcomeEmailRequested(
    Guid EventId,
    UserId UserId,
    string Email,
    DateTimeOffset OccurredAtUtc)
    : IIntegrationEvent;
