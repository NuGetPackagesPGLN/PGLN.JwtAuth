using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Domain.ExternalLogins;

public sealed class ExternalLogin
{
    private ExternalLogin()
    {
    }

    private ExternalLogin(
        ExternalLoginId id,
        UserId userId,
        ExternalLoginProvider provider,
        string providerSubject,
        string? email,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        UserId = userId;
        Provider = provider;
        ProviderSubject = providerSubject;
        Email = email;
        CreatedAtUtc = createdAtUtc;
        LastLoginAtUtc = createdAtUtc;
    }

    public ExternalLoginId Id { get; private set; }

    public UserId UserId { get; private set; }

    public ExternalLoginProvider Provider { get; private set; }

    public string ProviderSubject { get; private set; } =
        string.Empty;

    public string? Email { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset LastLoginAtUtc { get; private set; }

    public static ExternalLogin Create(
        UserId userId,
        ExternalLoginProvider provider,
        string providerSubject,
        string? email,
        DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(providerSubject))
        {
            throw new ArgumentException(
                "Provider subject is required.",
                nameof(providerSubject));
        }

        return new ExternalLogin(
            ExternalLoginId.New(),
            userId,
            provider,
            providerSubject.Trim(),
            NormalizeEmail(email),
            createdAtUtc);
    }

    public void RecordLogin(
        DateTimeOffset loggedInAtUtc,
        string? email = null)
    {
        LastLoginAtUtc = loggedInAtUtc;

        if (!string.IsNullOrWhiteSpace(email))
        {
            Email = NormalizeEmail(email);
        }
    }

    private static string? NormalizeEmail(
        string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return email.Trim().ToLowerInvariant();
    }
}
