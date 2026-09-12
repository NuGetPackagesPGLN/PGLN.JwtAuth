using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Abstractions.Authentication;

public interface IAccessTokenGenerator
{
    AccessTokenResult Generate(
        User user,
        AuthSessionId sessionId,
        DateTimeOffset issuedAtUtc);
}
