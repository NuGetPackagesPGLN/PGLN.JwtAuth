using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Events.Email;
using PGLN.Auth.Application.Features.Registration;
using PGLN.Auth.Application.Tests.TestDoubles;

namespace PGLN.Auth.Application.Tests.Features.Registration;

public sealed class RegisterCommandHandlerTests
{
    private static readonly DateTimeOffset FixedUtcNow =
        new(
            2026,
            9,
            6,
            8,
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
    public async Task HandleAsync_WithValidCommand_ShouldRegisterUser()
    {
        var userRepository =
            new FakeUserRepository();

        var tokenRepository =
            new FakeEmailVerificationTokenRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateHandler(
                userRepository,
                tokenRepository,
                unitOfWork: unitOfWork);

        var result =
            await handler.HandleAsync(
                new RegisterCommand(
                    "user@example.com",
                    "SecretPassword123!"));

        Assert.True(result.IsSuccess);

        Assert.NotNull(
            userRepository.AddedUser);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateHashedVerificationToken()
    {
        var userRepository =
            new FakeUserRepository();

        var tokenRepository =
            new FakeEmailVerificationTokenRepository();

        var generator =
            new FakeVerificationTokenGenerator
            {
                Token = "raw-token"
            };

        var handler =
            CreateHandler(
                userRepository,
                tokenRepository,
                verificationTokenGenerator: generator);

        var result =
            await handler.HandleAsync(
                new RegisterCommand(
                    "user@example.com",
                    "SecretPassword123!"));

        Assert.True(result.IsSuccess);

        Assert.NotNull(
            tokenRepository.AddedToken);

        Assert.Equal(
            "hashed::raw-token",
            tokenRepository.AddedToken.TokenHash);

        Assert.NotEqual(
            "raw-token",
            tokenRepository.AddedToken.TokenHash);
    }

    [Fact]
    public async Task HandleAsync_ShouldPublishEmailConfirmationRequested()
    {
        var userRepository =
            new FakeUserRepository();

        var tokenRepository =
            new FakeEmailVerificationTokenRepository();

        var eventPublisher =
            new FakeIntegrationEventPublisher();

        var generator =
            new FakeVerificationTokenGenerator
            {
                Token = "raw-email-token"
            };

        var handler =
            CreateHandler(
                userRepository,
                tokenRepository,
                verificationTokenGenerator: generator,
                eventPublisher: eventPublisher);

        var result =
            await handler.HandleAsync(
                new RegisterCommand(
                    "user@example.com",
                    "SecretPassword123!"));

        Assert.True(result.IsSuccess);

        var integrationEvent =
            Assert.Single(
                eventPublisher.Events);

        var confirmation =
            Assert.IsType<EmailConfirmationRequested>(
                integrationEvent);

        Assert.Equal(
            userRepository.AddedUser!.Id,
            confirmation.UserId);

        Assert.Equal(
            "user@example.com",
            confirmation.Email);

        Assert.Equal(
            "raw-email-token",
            confirmation.VerificationToken);

        Assert.Equal(
            FixedUtcNow,
            confirmation.OccurredAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailExists_ShouldNotPublishEvent()
    {
        var userRepository =
            new FakeUserRepository();

        var existing =
            PGLN.Auth.Domain.Users.User.Register(
                PGLN.Auth.Domain.Users.UserId.New(),
                PGLN.Auth.Domain.Users.Email.Create(
                    "user@example.com"),
                "existing-hash",
                FixedUtcNow);

        existing.ClearDomainEvents();

        userRepository.Seed(existing);

        var tokenRepository =
            new FakeEmailVerificationTokenRepository();

        var publisher =
            new FakeIntegrationEventPublisher();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateHandler(
                userRepository,
                tokenRepository,
                unitOfWork: unitOfWork,
                eventPublisher: publisher);

        var result =
            await handler.HandleAsync(
                new RegisterCommand(
                    "USER@example.com",
                    "SecretPassword123!"));

        Assert.True(result.IsFailure);

        Assert.Empty(
            publisher.Events);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotExposeVerificationTokenInResult()
    {
        var handler =
            CreateHandler(
                new FakeUserRepository(),
                new FakeEmailVerificationTokenRepository());

        var result =
            await handler.HandleAsync(
                new RegisterCommand(
                    "user@example.com",
                    "SecretPassword123!"));

        Assert.True(result.IsSuccess);

        Assert.Equal(
            "user@example.com",
            result.Value.Email);

        Assert.False(
            result.Value.EmailConfirmed);
    }

    private static RegisterCommandHandler CreateHandler(
        FakeUserRepository userRepository,
        FakeEmailVerificationTokenRepository tokenRepository,
        FakePasswordHasher? passwordHasher = null,
        FakeUnitOfWork? unitOfWork = null,
        FakeVerificationTokenGenerator? verificationTokenGenerator = null,
        FakeIntegrationEventPublisher? eventPublisher = null)
    {
        return new RegisterCommandHandler(
            userRepository,
            tokenRepository,
            passwordHasher ??
                new FakePasswordHasher(),
            verificationTokenGenerator ??
                new FakeVerificationTokenGenerator(),
            new FakeTokenHasher(),
            eventPublisher ??
                new FakeIntegrationEventPublisher(),
            unitOfWork ??
                new FakeUnitOfWork(),
            new FakeClock(
                FixedUtcNow),
            VerificationOptions);
    }
}
