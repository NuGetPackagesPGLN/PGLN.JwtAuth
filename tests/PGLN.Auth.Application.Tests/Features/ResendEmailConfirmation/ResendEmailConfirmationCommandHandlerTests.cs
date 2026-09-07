using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Events.Email;
using PGLN.Auth.Application.Features.ResendEmailConfirmation;
using PGLN.Auth.Application.Tests.TestDoubles;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.Domain.VerificationTokens;

namespace PGLN.Auth.Application.Tests.Features.ResendEmailConfirmation;

public sealed class ResendEmailConfirmationCommandHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            6,
            20,
            0,
            0,
            TimeSpan.Zero);

    private static readonly EmailVerificationOptions VerificationOptions =
        new()
        {
            TokenLifetime =
                TimeSpan.FromHours(24)
        };

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ShouldReturnSuccessWithoutSideEffects()
    {
        var userRepository =
            new FakeUserRepository();

        var tokenRepository =
            new FakeEmailVerificationTokenRepository();

        var generator =
            new FakeVerificationTokenGenerator();

        var publisher =
            new FakeIntegrationEventPublisher();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateHandler(
                userRepository,
                tokenRepository,
                generator,
                publisher,
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new ResendEmailConfirmationCommand(
                    "unknown@example.com"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            0,
            generator.GenerateCallCount);

        Assert.Null(
            tokenRepository.AddedToken);

        Assert.Empty(
            publisher.Events);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenUserAlreadyConfirmed_ShouldReturnSuccessWithoutSideEffects()
    {
        var userRepository =
            new FakeUserRepository();

        var tokenRepository =
            new FakeEmailVerificationTokenRepository();

        var generator =
            new FakeVerificationTokenGenerator();

        var publisher =
            new FakeIntegrationEventPublisher();

        var unitOfWork =
            new FakeUnitOfWork();

        var user =
            CreateUser();

        user.ConfirmEmail(
            Now.AddMinutes(-5));

        user.ClearDomainEvents();

        userRepository.Seed(
            user);

        var handler =
            CreateHandler(
                userRepository,
                tokenRepository,
                generator,
                publisher,
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new ResendEmailConfirmationCommand(
                    "USER@example.com"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            0,
            generator.GenerateCallCount);

        Assert.Null(
            tokenRepository.AddedToken);

        Assert.Empty(
            publisher.Events);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsUnconfirmed_ShouldRevokeExistingActiveTokens()
    {
        var userRepository =
            new FakeUserRepository();

        var tokenRepository =
            new FakeEmailVerificationTokenRepository();

        var user =
            CreateUser();

        userRepository.Seed(
            user);

        var firstToken =
            CreateToken(
                user.Id,
                "hashed::old-token-1");

        var secondToken =
            CreateToken(
                user.Id,
                "hashed::old-token-2");

        tokenRepository.Seed(
            firstToken);

        tokenRepository.Seed(
            secondToken);

        var handler =
            CreateHandler(
                userRepository,
                tokenRepository);

        var result =
            await handler.HandleAsync(
                new ResendEmailConfirmationCommand(
                    "user@example.com"));

        Assert.True(
            result.IsSuccess);

        Assert.True(
            firstToken.IsUsed);

        Assert.Equal(
            Now,
            firstToken.UsedAtUtc);

        Assert.True(
            secondToken.IsUsed);

        Assert.Equal(
            Now,
            secondToken.UsedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsUnconfirmed_ShouldCreateHashedReplacementToken()
    {
        var userRepository =
            new FakeUserRepository();

        var tokenRepository =
            new FakeEmailVerificationTokenRepository();

        var generator =
            new FakeVerificationTokenGenerator
            {
                Token =
                    "new-raw-token"
            };

        var user =
            CreateUser();

        userRepository.Seed(
            user);

        var handler =
            CreateHandler(
                userRepository,
                tokenRepository,
                generator);

        var result =
            await handler.HandleAsync(
                new ResendEmailConfirmationCommand(
                    "user@example.com"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            1,
            generator.GenerateCallCount);

        Assert.NotNull(
            tokenRepository.AddedToken);

        Assert.Equal(
            user.Id,
            tokenRepository.AddedToken.UserId);

        Assert.Equal(
            "hashed::new-raw-token",
            tokenRepository.AddedToken.TokenHash);

        Assert.NotEqual(
            "new-raw-token",
            tokenRepository.AddedToken.TokenHash);

        Assert.Equal(
            Now,
            tokenRepository.AddedToken.CreatedAtUtc);

        Assert.Equal(
            Now.AddHours(24),
            tokenRepository.AddedToken.ExpiresAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsUnconfirmed_ShouldPublishEmailConfirmationRequested()
    {
        var userRepository =
            new FakeUserRepository();

        var tokenRepository =
            new FakeEmailVerificationTokenRepository();

        var generator =
            new FakeVerificationTokenGenerator
            {
                Token =
                    "new-email-token"
            };

        var publisher =
            new FakeIntegrationEventPublisher();

        var user =
            CreateUser();

        userRepository.Seed(
            user);

        var handler =
            CreateHandler(
                userRepository,
                tokenRepository,
                generator,
                publisher);

        var result =
            await handler.HandleAsync(
                new ResendEmailConfirmationCommand(
                    "USER@example.com"));

        Assert.True(
            result.IsSuccess);

        var integrationEvent =
            Assert.Single(
                publisher.Events);

        var confirmation =
            Assert.IsType<EmailConfirmationRequested>(
                integrationEvent);

        Assert.Equal(
            user.Id,
            confirmation.UserId);

        Assert.Equal(
            "user@example.com",
            confirmation.Email);

        Assert.Equal(
            "new-email-token",
            confirmation.VerificationToken);

        Assert.Equal(
            Now,
            confirmation.OccurredAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsUnconfirmed_ShouldSaveChangesOnce()
    {
        var userRepository =
            new FakeUserRepository();

        var tokenRepository =
            new FakeEmailVerificationTokenRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var user =
            CreateUser();

        userRepository.Seed(
            user);

        var handler =
            CreateHandler(
                userRepository,
                tokenRepository,
                unitOfWork: unitOfWork);

        var result =
            await handler.HandleAsync(
                new ResendEmailConfirmationCommand(
                    "user@example.com"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    private static ResendEmailConfirmationCommandHandler CreateHandler(
        FakeUserRepository userRepository,
        FakeEmailVerificationTokenRepository tokenRepository,
        FakeVerificationTokenGenerator? generator = null,
        FakeIntegrationEventPublisher? publisher = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new ResendEmailConfirmationCommandHandler(
            userRepository,
            tokenRepository,
            generator ??
                new FakeVerificationTokenGenerator(),
            new FakeTokenHasher(),
            publisher ??
                new FakeIntegrationEventPublisher(),
            unitOfWork ??
                new FakeUnitOfWork(),
            new FakeClock(
                Now),
            VerificationOptions);
    }

    private static User CreateUser()
    {
        var user =
            User.Register(
                UserId.New(),
                Email.Create(
                    "user@example.com"),
                "hashed-password",
                Now.AddDays(-1));

        user.ClearDomainEvents();

        return user;
    }

    private static EmailVerificationToken CreateToken(
        UserId userId,
        string tokenHash)
    {
        return EmailVerificationToken.Create(
            EmailVerificationTokenId.New(),
            userId,
            tokenHash,
            Now.AddHours(-1),
            Now.AddHours(23));
    }
}
