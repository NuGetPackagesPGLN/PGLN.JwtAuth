using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Domain.ExternalLogins;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.EntityFrameworkCore.Persistence;
using PGLN.Auth.EntityFrameworkCore.Persistence.Repositories;

namespace PGLN.Auth.IntegrationTests.Persistence;

public sealed class ExternalLoginRepositoryTests
{
    [Fact]
    public async Task AddAndGetByProviderAndSubjectAsync_ShouldPersistExternalLogin()
    {
        await using var dbContext =
            CreateDbContext();

        await dbContext.Database.EnsureCreatedAsync();

        var now =
            DateTimeOffset.UtcNow;

        var user =
            User.Register(
                UserId.New(),
                Email.Create("user@example.com"),
                "HASH",
                now);

        await dbContext.Users.AddAsync(user);

        var externalLogin =
            ExternalLogin.Create(
                user.Id,
                ExternalLoginProvider.Google,
                "google-subject-123",
                "user@example.com",
                now);

        var repository =
            new ExternalLoginRepository(
                dbContext);

        await repository.AddAsync(
            externalLogin);

        await dbContext.SaveChangesAsync();

        var result =
            await repository
                .GetByProviderAndSubjectAsync(
                    ExternalLoginProvider.Google,
                    "google-subject-123");

        Assert.NotNull(result);

        Assert.Equal(
            externalLogin.Id,
            result.Id);

        Assert.Equal(
            user.Id,
            result.UserId);

        Assert.Equal(
            ExternalLoginProvider.Google,
            result.Provider);

        Assert.Equal(
            "google-subject-123",
            result.ProviderSubject);

        Assert.Equal(
            "user@example.com",
            result.Email);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnUsersExternalLogins()
    {
        await using var dbContext =
            CreateDbContext();

        await dbContext.Database.EnsureCreatedAsync();

        var now =
            DateTimeOffset.UtcNow;

        var user =
            User.Register(
                UserId.New(),
                Email.Create("user@example.com"),
                "HASH",
                now);

        await dbContext.Users.AddAsync(user);

        var google =
            ExternalLogin.Create(
                user.Id,
                ExternalLoginProvider.Google,
                "google-subject",
                "user@example.com",
                now);

        var github =
            ExternalLogin.Create(
                user.Id,
                ExternalLoginProvider.GitHub,
                "github-subject",
                "user@example.com",
                now);

        var repository =
            new ExternalLoginRepository(
                dbContext);

        await repository.AddAsync(google);
        await repository.AddAsync(github);

        await dbContext.SaveChangesAsync();

        var results =
            await repository.GetByUserIdAsync(
                user.Id);

        Assert.Equal(
            2,
            results.Count);

        Assert.Contains(
            results,
            login =>
                login.Provider ==
                ExternalLoginProvider.Google);

        Assert.Contains(
            results,
            login =>
                login.Provider ==
                ExternalLoginProvider.GitHub);
    }

    [Fact]
    public async Task DuplicateProviderAndSubject_ShouldViolateUniqueConstraint()
    {
        await using var dbContext =
            CreateDbContext();

        await dbContext.Database.EnsureCreatedAsync();

        var now =
            DateTimeOffset.UtcNow;

        var firstUser =
            User.Register(
                UserId.New(),
                Email.Create("first@example.com"),
                "HASH",
                now);

        var secondUser =
            User.Register(
                UserId.New(),
                Email.Create("second@example.com"),
                "HASH",
                now);

        await dbContext.Users.AddRangeAsync(
            firstUser,
            secondUser);

        var firstLogin =
            ExternalLogin.Create(
                firstUser.Id,
                ExternalLoginProvider.Google,
                "same-google-subject",
                "first@example.com",
                now);

        var secondLogin =
            ExternalLogin.Create(
                secondUser.Id,
                ExternalLoginProvider.Google,
                "same-google-subject",
                "second@example.com",
                now);

        await dbContext.ExternalLogins.AddAsync(
            firstLogin);

        await dbContext.SaveChangesAsync();

        await dbContext.ExternalLogins.AddAsync(
            secondLogin);

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                dbContext.SaveChangesAsync());
    }

    private static AuthDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<AuthDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;

        var dbContext =
            new AuthDbContext(
                options);

        dbContext.Database.OpenConnection();

        return dbContext;
    }
}
