using PGLN.Auth.Application.Events.Email;
using PGLN.Auth.Application.Features.ChangePassword;
using PGLN.Auth.Application.Tests.TestDoubles;
using PGLN.Auth.Domain.RefreshTokens;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Tests.Features.ChangePassword;

public sealed class ChangePasswordCommandHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            10,
            6,
            30,
            0,
            TimeSpan.Zero);

    private const string EmailAddress =
        "user@example.com";

    private const string CurrentPassword =
        "CurrentPassword123!";

    private const string CurrentPasswordHash =
        "hashed::CurrentPassword123!";

    private const string NewPassword =
        "NewPassword456!";

    private const string NewPasswordHash =
        "hashed::NewPassword456!";

    [Fact]
    public async Task HandleAsync_WithValidRequest_ShouldChangePassword()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var passwordHasher =
            new FakePasswordHasher
            {
                HashResult =
                    NewPasswordHash,
                VerifyResult =
                    true
            };

        var result =
            await CreateHandler(
                    users,
                    passwordHasher:
                        passwordHasher)
                .HandleAsync(
                    CreateCommand(
                        user.Id));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            NewPasswordHash,
            user.PasswordHash);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidCurrentPassword_ShouldFail()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var passwordHasher =
            new FakePasswordHasher
            {
                VerifyResult =
                    false
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    users,
                    passwordHasher:
                        passwordHasher,
                    unitOfWork:
                        unitOfWork)
                .HandleAsync(
                    CreateCommand(
                        user.Id));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ChangePasswordErrors.InvalidCurrentPassword,
            result.Error);

        Assert.Equal(
            CurrentPasswordHash,
            user.PasswordHash);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithMissingUser_ShouldFail()
    {
        var users =
            new FakeUserRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    users,
                    unitOfWork:
                        unitOfWork)
                .HandleAsync(
                    new ChangePasswordCommand(
                        UserId.New().Value.ToString(),
                        CurrentPassword,
                        NewPassword));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ChangePasswordErrors.UserNotFound,
            result.Error);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithValidRequest_ShouldRevokeActiveRefreshTokens()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var refreshTokens =
            new FakeRefreshTokenRepository();

        var activeToken =
            CreateRefreshToken(
                user.Id);

        refreshTokens.Seed(
            activeToken);

        var result =
            await CreateHandler(
                    users,
                    refreshTokens:
                        refreshTokens)
                .HandleAsync(
                    CreateCommand(
                        user.Id));

        Assert.True(
            result.IsSuccess);

        Assert.True(
            activeToken.IsRevoked);

        Assert.Equal(
            "PasswordChanged",
            activeToken.RevocationReason);

        Assert.Equal(
            Now,
            activeToken.RevokedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotOverwriteAlreadyRevokedRefreshToken()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var refreshTokens =
            new FakeRefreshTokenRepository();

        var token =
            CreateRefreshToken(
                user.Id);

        var originalRevokedAt =
            Now.AddMinutes(-2);

        token.Revoke(
            originalRevokedAt,
            "UserLogout");

        refreshTokens.Seed(
            token);

        var result =
            await CreateHandler(
                    users,
                    refreshTokens:
                        refreshTokens)
                .HandleAsync(
                    CreateCommand(
                        user.Id));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            "UserLogout",
            token.RevocationReason);

        Assert.Equal(
            originalRevokedAt,
            token.RevokedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WithValidRequest_ShouldSaveExactlyOnce()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    users,
                    unitOfWork:
                        unitOfWork)
                .HandleAsync(
                    CreateCommand(
                        user.Id));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }


    [Fact]
    public async Task HandleAsync_WithValidRequest_ShouldPublishPasswordChangedNotification()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var integrationEventPublisher =
            new FakeIntegrationEventPublisher();

        var result =
            await CreateHandler(
                    users,
                    integrationEventPublisher:
                        integrationEventPublisher)
                .HandleAsync(
                    CreateCommand(
                        user.Id));

        Assert.True(
            result.IsSuccess);

        var integrationEvent =
            Assert.Single(
                integrationEventPublisher.Events);

        var passwordChanged =
            Assert.IsType<PasswordChangedNotificationRequested>(
                integrationEvent);

        Assert.Equal(
            user.Id,
            passwordChanged.UserId);

        Assert.Equal(
            EmailAddress,
            passwordChanged.Email);

        Assert.Equal(
            Now,
            passwordChanged.OccurredAtUtc);
    }
    private static ChangePasswordCommandHandler CreateHandler(
        FakeUserRepository users,
        FakeRefreshTokenRepository? refreshTokens = null,
        FakePasswordHasher? passwordHasher = null,
        FakeIntegrationEventPublisher? integrationEventPublisher = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        refreshTokens ??=
            new FakeRefreshTokenRepository();

        passwordHasher ??=
            CreatePasswordHasher();

        integrationEventPublisher ??=
            new FakeIntegrationEventPublisher();

        unitOfWork ??=
            new FakeUnitOfWork();

        return new ChangePasswordCommandHandler(
            users,
            refreshTokens,
            passwordHasher,
            integrationEventPublisher,
            unitOfWork,
            new FakeClock(
                Now));
    }

    private static FakePasswordHasher CreatePasswordHasher()
    {
        return new FakePasswordHasher
        {
            HashResult =
                NewPasswordHash,
            VerifyResult =
                true
        };
    }

    private static ChangePasswordCommand CreateCommand(
        UserId userId)
    {
        return new ChangePasswordCommand(
            userId.Value.ToString(),
            CurrentPassword,
            NewPassword);
    }

    private static User CreateUser()
    {
        var user =
            User.Register(
                UserId.New(),
                Email.Create(
                    EmailAddress),
                CurrentPasswordHash,
                Now.AddDays(-30));

        user.ClearDomainEvents();

        return user;
    }

    private static RefreshToken CreateRefreshToken(
        UserId userId)
    {
        return RefreshToken.Create(
            RefreshTokenId.New(),
            RefreshTokenFamilyId.New(),
            userId,
            AuthSessionId.New(),
            Guid.NewGuid().ToString("N"),
            Now.AddMinutes(-5),
            Now.AddDays(7));
    }
}





