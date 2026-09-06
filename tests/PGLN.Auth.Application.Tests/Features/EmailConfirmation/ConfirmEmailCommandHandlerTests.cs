using PGLN.Auth.Application.Features.EmailConfirmation;
using PGLN.Auth.Application.Tests.TestDoubles;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.Domain.Users.Events;
using PGLN.Auth.Domain.VerificationTokens;

namespace PGLN.Auth.Application.Tests.Features.EmailConfirmation;

public sealed class ConfirmEmailCommandHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            6,
            12,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_WithValidToken_ShouldConfirmEmail()
    {
        var userRepository =
            new FakeUserRepository();

        var tokenRepository =
            new FakeEmailVerificationTokenRepository();

        var user =
            CreateUser();

        user.ClearDomainEvents();

        userRepository.Seed(user);

        var token =
            EmailVerificationToken.Create(
                EmailVerificationTokenId.New(),
                user.Id,
                "hashed::raw-token",
                Now.AddHours(-1),
                Now.AddHours(23));

        tokenRepository.Seed(token);

        var handler =
            CreateHandler(
                tokenRepository,
                userRepository);

        var result =
            await handler.HandleAsync(
                new ConfirmEmailCommand(
                    "raw-token"));

        Assert.True(result.IsSuccess);

        Assert.True(user.EmailConfirmed);

        Assert.Equal(
            Now,
            user.EmailConfirmedAtUtc);

        Assert.True(token.IsUsed);

        Assert.Equal(
            Now,
            token.UsedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WithValidToken_ShouldRaiseEmailConfirmedEvent()
    {
        var userRepository =
            new FakeUserRepository();

        var tokenRepository =
            new FakeEmailVerificationTokenRepository();

        var user =
            CreateUser();

        user.ClearDomainEvents();

        userRepository.Seed(user);

        var token =
            CreateToken(
                user.Id);

        tokenRepository.Seed(token);

        var handler =
            CreateHandler(
                tokenRepository,
                userRepository);

        var result =
            await handler.HandleAsync(
                new ConfirmEmailCommand(
                    "raw-token"));

        Assert.True(result.IsSuccess);

        var domainEvent =
            Assert.Single(
                user.DomainEvents);

        var confirmedEvent =
            Assert.IsType<EmailConfirmed>(
                domainEvent);

        Assert.Equal(
            user.Id,
            confirmedEvent.UserId);

        Assert.Equal(
            user.Email.Value,
            confirmedEvent.Email);

        Assert.Equal(
            Now,
            confirmedEvent.ConfirmedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownToken_ShouldReturnInvalidToken()
    {
        var userRepository =
            new FakeUserRepository();

        var tokenRepository =
            new FakeEmailVerificationTokenRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateHandler(
                tokenRepository,
                userRepository,
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new ConfirmEmailCommand(
                    "unknown-token"));

        Assert.True(result.IsFailure);

        Assert.Equal(
            EmailConfirmationErrors.InvalidToken,
            result.Error);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithExpiredToken_ShouldReturnExpiredToken()
    {
        var userRepository =
            new FakeUserRepository();

        var tokenRepository =
            new FakeEmailVerificationTokenRepository();

        var user =
            CreateUser();

        userRepository.Seed(user);

        var token =
            EmailVerificationToken.Create(
                EmailVerificationTokenId.New(),
                user.Id,
                "hashed::raw-token",
                Now.AddDays(-2),
                Now.AddSeconds(-1));

        tokenRepository.Seed(token);

        var result =
            await CreateHandler(
                    tokenRepository,
                    userRepository)
                .HandleAsync(
                    new ConfirmEmailCommand(
                        "raw-token"));

        Assert.True(result.IsFailure);

        Assert.Equal(
            EmailConfirmationErrors.ExpiredToken,
            result.Error);

        Assert.False(user.EmailConfirmed);
    }

    [Fact]
    public async Task HandleAsync_WithUsedToken_ShouldReturnTokenAlreadyUsed()
    {
        var userRepository =
            new FakeUserRepository();

        var tokenRepository =
            new FakeEmailVerificationTokenRepository();

        var user =
            CreateUser();

        userRepository.Seed(user);

        var token =
            CreateToken(
                user.Id);

        token.MarkAsUsed(
            Now.AddMinutes(-1));

        tokenRepository.Seed(token);

        var result =
            await CreateHandler(
                    tokenRepository,
                    userRepository)
                .HandleAsync(
                    new ConfirmEmailCommand(
                        "raw-token"));

        Assert.True(result.IsFailure);

        Assert.Equal(
            EmailConfirmationErrors.TokenAlreadyUsed,
            result.Error);
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ShouldReturnUserNotFound()
    {
        var userRepository =
            new FakeUserRepository();

        var tokenRepository =
            new FakeEmailVerificationTokenRepository();

        var token =
            CreateToken(
                UserId.New());

        tokenRepository.Seed(token);

        var result =
            await CreateHandler(
                    tokenRepository,
                    userRepository)
                .HandleAsync(
                    new ConfirmEmailCommand(
                        "raw-token"));

        Assert.True(result.IsFailure);

        Assert.Equal(
            EmailConfirmationErrors.UserNotFound,
            result.Error);
    }

    [Fact]
    public async Task HandleAsync_WithValidToken_ShouldSaveChangesOnce()
    {
        var userRepository =
            new FakeUserRepository();

        var tokenRepository =
            new FakeEmailVerificationTokenRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var user =
            CreateUser();

        userRepository.Seed(user);

        tokenRepository.Seed(
            CreateToken(user.Id));

        var handler =
            CreateHandler(
                tokenRepository,
                userRepository,
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new ConfirmEmailCommand(
                    "raw-token"));

        Assert.True(result.IsSuccess);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    private static ConfirmEmailCommandHandler CreateHandler(
        FakeEmailVerificationTokenRepository tokenRepository,
        FakeUserRepository userRepository,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new ConfirmEmailCommandHandler(
            tokenRepository,
            userRepository,
            new FakeTokenHasher(),
            new FakeClock(Now),
            unitOfWork ??
            new FakeUnitOfWork());
    }

    private static User CreateUser()
    {
        return User.Register(
            UserId.New(),
            Email.Create(
                "user@example.com"),
            "hashed-password",
            Now.AddDays(-1));
    }

    private static EmailVerificationToken CreateToken(
        UserId userId)
    {
        return EmailVerificationToken.Create(
            EmailVerificationTokenId.New(),
            userId,
            "hashed::raw-token",
            Now.AddHours(-1),
            Now.AddHours(23));
    }
}
