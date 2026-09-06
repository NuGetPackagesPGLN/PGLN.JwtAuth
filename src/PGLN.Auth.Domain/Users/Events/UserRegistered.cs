using PGLN.Auth.Domain.Common;

namespace PGLN.Auth.Domain.Users.Events;

public sealed record UserRegistered(
    UserId UserId,
    string Email,
    DateTimeOffset RegisteredAtUtc)
    : DomainEvent;