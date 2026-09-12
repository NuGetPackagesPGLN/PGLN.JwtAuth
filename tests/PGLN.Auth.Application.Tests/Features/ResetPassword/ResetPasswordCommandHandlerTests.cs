using PGLN.Auth.Application.Features.ResetPassword;
using PGLN.Auth.Application.Tests.TestDoubles;
using PGLN.Auth.Domain.PasswordResets;
using PGLN.Auth.Domain.RefreshTokens;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Tests.Features.ResetPassword;

public sealed class ResetPasswordCommandHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            9,
            19,
            0,
            0,
            TimeSpan.Zero);

    private const string EmailAddress =
        "user@example.com";

    private const string RawResetToken =
        "reset-token";

    private const string ResetTokenHash =
        "hashed::reset-token";

    private const string NewPassword =
        "NewPassword123!";

    [Fact]
    public async Task HandleAsync_WithValidRequest_ShouldChangePassword()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var resetTokens =
            new FakePasswordResetTokenRepository();

        var resetToken =
            CreateResetToken(
                user.Id);

        resetTokens.Seed(
            resetToken);

        var passwordHasher =
            new FakePasswordHasher();

        var result =
            await CreateHandler(
                    users,
                    resetTokens,
                    passwordHasher:
                        passwordHasher)
                .HandleAsync(
                    CreateCommand());

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            NewPassword,
            passwordHasher.LastPasswordHashed);

        Assert.Equal(
            passwordHasher.HashResult,
            user.PasswordHash);
    }

    [Fact]
    public async Task HandleAsync_WithValidRequest_ShouldMarkResetTokenAsUsed()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var resetTokens =
            new FakePasswordResetTokenRepository();

        var resetToken =
            CreateResetToken(
                user.Id);

        resetTokens.Seed(
            resetToken);

        var result =
            await CreateHandler(
                    users,
                    resetTokens)
                .HandleAsync(
                    CreateCommand());

        Assert.True(
            result.IsSuccess);

        Assert.True(
            resetToken.IsUsed);

        Assert.Equal(
            Now,
            resetToken.UsedAtUtc);
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

        var resetTokens =
            new FakePasswordResetTokenRepository();

        resetTokens.Seed(
            CreateResetToken(
                user.Id));

        var refreshTokens =
            new FakeRefreshTokenRepository();

        var first =
            CreateActiveRefreshToken(
                user.Id,
                "hashed::refresh-one");

        var second =
            CreateActiveRefreshToken(
                user.Id,
                "hashed::refresh-two");

        refreshTokens.Seed(
            first);

        refreshTokens.Seed(
            second);

        var result =
            await CreateHandler(
                    users,
                    resetTokens,
                    refreshTokens)
                .HandleAsync(
                    CreateCommand());

        Assert.True(
            result.IsSuccess);

        Assert.True(
            first.IsRevoked);

        Assert.True(
            second.IsRevoked);

        Assert.Equal(
            "PasswordReset",
            first.RevocationReason);

        Assert.Equal(
            "PasswordReset",
            second.RevocationReason);

        Assert.Equal(
            Now,
            first.RevokedAtUtc);

        Assert.Equal(
            Now,
            second.RevokedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotRevokeAnotherUsersRefreshTokens()
    {
        var user =
            CreateUser();

        var otherUser =
            CreateUser(
                "other@example.com");

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        users.Seed(
            otherUser);

        var resetTokens =
            new FakePasswordResetTokenRepository();

        resetTokens.Seed(
            CreateResetToken(
                user.Id));

        var refreshTokens =
            new FakeRefreshTokenRepository();

        var targetToken =
            CreateActiveRefreshToken(
                user.Id,
                "hashed::target-refresh");

        var unrelatedToken =
            CreateActiveRefreshToken(
                otherUser.Id,
                "hashed::other-refresh");

        refreshTokens.Seed(
            targetToken);

        refreshTokens.Seed(
            unrelatedToken);

        var result =
            await CreateHandler(
                    users,
                    resetTokens,
                    refreshTokens)
                .HandleAsync(
                    CreateCommand());

        Assert.True(
            result.IsSuccess);

        Assert.True(
            targetToken.IsRevoked);

        Assert.False(
            unrelatedToken.IsRevoked);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotModifyAlreadyRevokedRefreshToken()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var resetTokens =
            new FakePasswordResetTokenRepository();

        resetTokens.Seed(
            CreateResetToken(
                user.Id));

        var refreshTokens =
            new FakeRefreshTokenRepository();

        var refreshToken =
            CreateActiveRefreshToken(
                user.Id,
                "hashed::already-revoked");

        var originalRevokedAt =
            Now.AddHours(-1);

        refreshToken.Revoke(
            originalRevokedAt,
            "Logout");

        refreshTokens.Seed(
            refreshToken);

        var result =
            await CreateHandler(
                    users,
                    resetTokens,
                    refreshTokens)
                .HandleAsync(
                    CreateCommand());

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            "Logout",
            refreshToken.RevocationReason);

        Assert.Equal(
            originalRevokedAt,
            refreshToken.RevokedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotRevokeExpiredRefreshToken()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var resetTokens =
            new FakePasswordResetTokenRepository();

        resetTokens.Seed(
            CreateResetToken(
                user.Id));

        var refreshTokens =
            new FakeRefreshTokenRepository();

        var expiredRefreshToken =
            RefreshToken.Create(
                RefreshTokenId.New(),
                RefreshTokenFamilyId.New(),
                user.Id, AuthSessionId.New(),
                "hashed::expired-refresh",
                Now.AddDays(-31),
                Now.AddSeconds(-1));

        refreshTokens.Seed(
            expiredRefreshToken);

        var result =
            await CreateHandler(
                    users,
                    resetTokens,
                    refreshTokens)
                .HandleAsync(
                    CreateCommand());

        Assert.True(
            result.IsSuccess);

        Assert.False(
            expiredRefreshToken.IsRevoked);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownToken_ShouldFail()
    {
        var users =
            new FakeUserRepository();

        users.Seed(
            CreateUser());

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    users,
                    new FakePasswordResetTokenRepository(),
                    unitOfWork:
                        unitOfWork)
                .HandleAsync(
                    CreateCommand());

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ResetPasswordErrors.InvalidToken,
            result.Error);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithExpiredToken_ShouldFail()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var resetTokens =
            new FakePasswordResetTokenRepository();

        resetTokens.Seed(
            PasswordResetToken.Create(
                PasswordResetTokenId.New(),
                user.Id,
                ResetTokenHash,
                Now.AddHours(-1),
                Now.AddSeconds(-1)));

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    users,
                    resetTokens,
                    unitOfWork:
                        unitOfWork)
                .HandleAsync(
                    CreateCommand());

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ResetPasswordErrors.ExpiredToken,
            result.Error);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);

        Assert.Equal(
            "old-password-hash",
            user.PasswordHash);
    }

    [Fact]
    public async Task HandleAsync_WithUsedToken_ShouldFail()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var resetTokens =
            new FakePasswordResetTokenRepository();

        var resetToken =
            CreateResetToken(
                user.Id);

        resetToken.MarkAsUsed(
            Now.AddMinutes(-1));

        resetTokens.Seed(
            resetToken);

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    users,
                    resetTokens,
                    unitOfWork:
                        unitOfWork)
                .HandleAsync(
                    CreateCommand());

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ResetPasswordErrors.UsedToken,
            result.Error);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);

        Assert.Equal(
            "old-password-hash",
            user.PasswordHash);
    }

    [Fact]
    public async Task HandleAsync_WhenTokenBelongsToAnotherUser_ShouldFail()
    {
        var user =
            CreateUser();

        var otherUser =
            CreateUser(
                "other@example.com");

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        users.Seed(
            otherUser);

        var resetTokens =
            new FakePasswordResetTokenRepository();

        var resetToken =
            CreateResetToken(
                otherUser.Id);

        resetTokens.Seed(
            resetToken);

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    users,
                    resetTokens,
                    unitOfWork:
                        unitOfWork)
                .HandleAsync(
                    CreateCommand());

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ResetPasswordErrors.InvalidRequest,
            result.Error);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);

        Assert.False(
            resetToken.IsUsed);

        Assert.Equal(
            "old-password-hash",
            user.PasswordHash);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownEmail_ShouldFail()
    {
        var tokenOwner =
            CreateUser(
                "owner@example.com");

        var resetTokens =
            new FakePasswordResetTokenRepository();

        var resetToken =
            CreateResetToken(
                tokenOwner.Id);

        resetTokens.Seed(
            resetToken);

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    new FakeUserRepository(),
                    resetTokens,
                    unitOfWork:
                        unitOfWork)
                .HandleAsync(
                    CreateCommand());

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ResetPasswordErrors.InvalidRequest,
            result.Error);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);

        Assert.False(
            resetToken.IsUsed);
    }

    [Fact]
    public async Task HandleAsync_WithValidRequest_ShouldSaveChangesOnce()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var resetTokens =
            new FakePasswordResetTokenRepository();

        resetTokens.Seed(
            CreateResetToken(
                user.Id));

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    users,
                    resetTokens,
                    unitOfWork:
                        unitOfWork)
                .HandleAsync(
                    CreateCommand());

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    private static ResetPasswordCommandHandler CreateHandler(
        FakeUserRepository userRepository,
        FakePasswordResetTokenRepository passwordResetTokenRepository,
        FakeRefreshTokenRepository? refreshTokenRepository = null,
        FakePasswordHasher? passwordHasher = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new ResetPasswordCommandHandler(
            userRepository,
            passwordResetTokenRepository,
            refreshTokenRepository ??
                new FakeRefreshTokenRepository(),
            passwordHasher ??
                new FakePasswordHasher(),
            new FakeTokenHasher(),
            unitOfWork ??
                new FakeUnitOfWork(),
            new FakeClock(
                Now));
    }

    private static ResetPasswordCommand CreateCommand()
    {
        return new ResetPasswordCommand(
            EmailAddress,
            RawResetToken,
            NewPassword);
    }

    private static User CreateUser(
        string email =
            EmailAddress)
    {
        var user =
            User.Register(
                UserId.New(),
                Email.Create(
                    email),
                "old-password-hash",
                Now.AddDays(-30));

        user.ClearDomainEvents();

        return user;
    }

    private static PasswordResetToken CreateResetToken(
        UserId userId)
    {
        return PasswordResetToken.Create(
            PasswordResetTokenId.New(),
            userId,
            ResetTokenHash,
            Now.AddMinutes(-5),
            Now.AddMinutes(25));
    }

    private static RefreshToken CreateActiveRefreshToken(
        UserId userId,
        string tokenHash)
    {
        return RefreshToken.Create(
            RefreshTokenId.New(),
            RefreshTokenFamilyId.New(),
            userId,
            AuthSessionId.New(),
            tokenHash,
            Now.AddDays(-1),
            Now.AddDays(29));
    }
}




