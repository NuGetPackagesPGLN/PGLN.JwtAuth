using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Features.Registration;

public sealed record RegisterResult(
    UserId UserId,
    string Email,
    bool EmailConfirmed);
