using PGLN.Auth.Domain.Common;

namespace PGLN.Auth.Domain.Users.Events;

public sealed record PasswordChanged(
    UserId UserId,
    DateTimeOffset ChangedAtUtc)
    : DomainEvent;