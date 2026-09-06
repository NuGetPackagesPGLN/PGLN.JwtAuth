using PGLN.Auth.Domain.Common;

namespace PGLN.Auth.Domain.Users.Events;

public sealed record EmailConfirmed(
    UserId UserId,
    string Email,
    DateTimeOffset ConfirmedAtUtc)
    : DomainEvent;