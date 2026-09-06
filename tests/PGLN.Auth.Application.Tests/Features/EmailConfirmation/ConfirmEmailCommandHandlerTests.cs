using PGLN.Auth.Application.Events.Email;
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
            18,
            30,
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
            CreateToken(
                user.Id);

        tokenRepository.Seed(token);

        var publisher =
            new FakeIntegrationEventPublisher();

        var handler =
            CreateHandler(
                tokenRepository,
                userRepository,
                publisher);

        var result =
            await handler.HandleAsync(
                new ConfirmEmailCommand(
                    "raw-token"));

        Assert.True(
            result.IsSuccess);

        Assert.True(
            user.EmailConfirmed);

        Assert.Equal(
            Now,
            user.EmailConfirmedAtUtc);

        Assert.True(
            token.IsUsed);

        Assert.Equal(
            Now,
            token.UsedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WithValidToken_ShouldRaiseEmailConfirmedDomainEvent()
    {
        var userRepository =
            new FakeUserRepository();

        var tokenRepository =
            new FakeEmailVerificationTokenRepository();

        var user =
            CreateUser();

        user.ClearDomainEvents();

        userRepository.Seed(user);

        tokenRepository.Seed(
            CreateToken(
                user.Id));

        var handler =
            CreateHandler(
                tokenRepository,
                userRepository);

        var result =
            await handler.HandleAsync(
                new ConfirmEmailCommand(
                    "raw-token"));

        Assert.True(
            result.IsSuccess);

        var domainEvent =
            Assert.Single(
                user.DomainEvents);

        var emailConfirmed =
            Assert.IsType<EmailConfirmed>(
                domainEvent);

        Assert.Equal(
            user.Id,
            emailConfirmed.UserId);

        Assert.Equal(
            user.Email.Value,
            emailConfirmed.Email);

        Assert.Equal(
            Now,
            emailConfirmed.ConfirmedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WithValidToken_ShouldPublishWelcomeEmailRequested()
    {
        var userRepository =
            new FakeUserRepository();

        var tokenRepository =
            new FakeEmailVerificationTokenRepository();

        var publisher =
            new FakeIntegrationEventPublisher();

        var user =
            CreateUser();

        user.ClearDomainEvents();

        userRepository.Seed(user);

        tokenRepository.Seed(
            CreateToken(
                user.Id));

        var handler =
            CreateHandler(
                tokenRepository,
                userRepository,
                publisher);

        var result =
            await handler.HandleAsync(
                new ConfirmEmailCommand(
                    "raw-token"));

        Assert.True(
            result.IsSuccess);

        var integrationEvent =
            Assert.Single(
                publisher.Events);

        var welcome =
            Assert.IsType<WelcomeEmailRequested>(
                integrationEvent);

        Assert.Equal(
            user.Id,
            welcome.UserId);

        Assert.Equal(
            user.Email.Value,
            welcome.Email);

        Assert.Equal(
            Now,
            welcome.OccurredAtUtc);
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
            CreateToken(
                user.Id));

        var handler =
            CreateHandler(
                tokenRepository,
                userRepository,
                unitOfWork: unitOfWork);

        var result =
            await handler.HandleAsync(
                new ConfirmEmailCommand(
                    "raw-token"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownToken_ShouldNotPublishWelcomeEmail()
    {
        var publisher =
            new FakeIntegrationEventPublisher();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateHandler(
                new FakeEmailVerificationTokenRepository(),
                new FakeUserRepository(),
                publisher,
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new ConfirmEmailCommand(
                    "unknown-token"));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            EmailConfirmationErrors.InvalidToken,
            result.Error);

        Assert.Empty(
            publisher.Events);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithExpiredToken_ShouldNotPublishWelcomeEmail()
    {
        var userRepository =
            new FakeUserRepository();

        var tokenRepository =
            new FakeEmailVerificationTokenRepository();

        var publisher =
            new FakeIntegrationEventPublisher();

        var user =
            CreateUser();

        userRepository.Seed(user);

        tokenRepository.Seed(
            EmailVerificationToken.Create(
                EmailVerificationTokenId.New(),
                user.Id,
                "hashed::raw-token",
                Now.AddDays(-2),
                Now.AddSeconds(-1)));

        var result =
            await CreateHandler(
                    tokenRepository,
                    userRepository,
                    publisher)
                .HandleAsync(
                    new ConfirmEmailCommand(
                        "raw-token"));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            EmailConfirmationErrors.ExpiredToken,
            result.Error);

        Assert.Empty(
            publisher.Events);

        Assert.False(
            user.EmailConfirmed);
    }

    [Fact]
    public async Task HandleAsync_WithUsedToken_ShouldNotPublishWelcomeEmail()
    {
        var userRepository =
            new FakeUserRepository();

        var tokenRepository =
            new FakeEmailVerificationTokenRepository();

        var publisher =
            new FakeIntegrationEventPublisher();

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
                    userRepository,
                    publisher)
                .HandleAsync(
                    new ConfirmEmailCommand(
                        "raw-token"));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            EmailConfirmationErrors.TokenAlreadyUsed,
            result.Error);

        Assert.Empty(
            publisher.Events);
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ShouldNotPublishWelcomeEmail()
    {
        var tokenRepository =
            new FakeEmailVerificationTokenRepository();

        var publisher =
            new FakeIntegrationEventPublisher();

        tokenRepository.Seed(
            CreateToken(
                UserId.New()));

        var result =
            await CreateHandler(
                    tokenRepository,
                    new FakeUserRepository(),
                    publisher)
                .HandleAsync(
                    new ConfirmEmailCommand(
                        "raw-token"));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            EmailConfirmationErrors.UserNotFound,
            result.Error);

        Assert.Empty(
            publisher.Events);
    }

    private static ConfirmEmailCommandHandler CreateHandler(
        FakeEmailVerificationTokenRepository tokenRepository,
        FakeUserRepository userRepository,
        FakeIntegrationEventPublisher? publisher = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new ConfirmEmailCommandHandler(
            tokenRepository,
            userRepository,
            new FakeTokenHasher(),
            publisher ??
                new FakeIntegrationEventPublisher(),
            new FakeClock(
                Now),
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
