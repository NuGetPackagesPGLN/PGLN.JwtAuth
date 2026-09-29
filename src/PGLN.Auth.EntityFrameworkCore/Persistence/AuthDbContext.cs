using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Domain.EmailChangeTokens;
using PGLN.Auth.Domain.LoginAttempts;
using PGLN.Auth.Domain.PasswordResets;
using PGLN.Auth.Domain.RefreshTokens;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.StepUpChallenges;
using PGLN.Auth.Domain.TrustedDevices;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.Domain.ExternalLogins;
using PGLN.Auth.Domain.VerificationTokens;
using PGLN.Auth.EntityFrameworkCore.Inbox;
using PGLN.Auth.EntityFrameworkCore.Outbox;

namespace PGLN.Auth.EntityFrameworkCore.Persistence;

public sealed class AuthDbContext
    : DbContext
{
    public AuthDbContext(
        DbContextOptions<AuthDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users =>
        Set<User>();

    public DbSet<EmailVerificationToken> EmailVerificationTokens =>
        Set<EmailVerificationToken>();

    public DbSet<EmailChangeToken> EmailChangeTokens =>
        Set<EmailChangeToken>();

    public DbSet<OutboxMessage> OutboxMessages =>
        Set<OutboxMessage>();

    public DbSet<InboxMessage> InboxMessages =>
        Set<InboxMessage>();

    public DbSet<RefreshToken> RefreshTokens =>
        Set<RefreshToken>();

    public DbSet<AuthSession> AuthSessions =>
        Set<AuthSession>();

    public DbSet<TrustedDevice> TrustedDevices =>
        Set<TrustedDevice>();

    public DbSet<ExternalLogin> ExternalLogins =>
        Set<ExternalLogin>();

    public DbSet<StepUpChallenge> StepUpChallenges =>
        Set<StepUpChallenge>();

    public DbSet<PasswordResetToken> PasswordResetTokens =>
        Set<PasswordResetToken>();

    public DbSet<LoginAttempt> LoginAttempts =>
        Set<LoginAttempt>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(
            modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AuthDbContext).Assembly);
    }
}
