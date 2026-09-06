using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Features.EmailConfirmation;

public sealed record ConfirmEmailResult(
    UserId UserId,
    string Email,
    bool EmailConfirmed);
