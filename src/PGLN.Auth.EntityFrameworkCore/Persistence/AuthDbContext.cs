using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.Domain.VerificationTokens;
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

    public DbSet<OutboxMessage> OutboxMessages =>
        Set<OutboxMessage>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AuthDbContext).Assembly);
    }
}
