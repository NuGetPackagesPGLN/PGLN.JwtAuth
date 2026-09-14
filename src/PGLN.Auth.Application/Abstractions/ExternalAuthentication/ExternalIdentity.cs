using PGLN.Auth.Domain.ExternalLogins;

namespace PGLN.Auth.Application.Abstractions.ExternalAuthentication;

public sealed record ExternalIdentity(
    ExternalLoginProvider Provider,
    string ProviderSubject,
    string? Email,
    bool EmailVerified,
    string? DisplayName);
