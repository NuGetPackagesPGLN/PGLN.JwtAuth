using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Features.Registration;
using PGLN.Auth.Application.Tests.TestDoubles;
using PGLN.Auth.Domain.Users.Events;

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

        var passwordHasher =
            new FakePasswordHasher
            {
                HashResult =
                    "hashed-secret-password"
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateHandler(
                userRepository,
                tokenRepository,
                passwordHasher,
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new RegisterCommand(
                    "user@example.com",
                    "SecretPassword123!"));

        Assert.True(result.IsSuccess);

        Assert.NotNull(
            userRepository.AddedUser);

        Assert.Equal(
            "hashed-secret-password",
            userRepository.AddedUser.PasswordHash);

        Assert.Equal(
            FixedUtcNow,
            userRepository.AddedUser.CreatedAtUtc);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_ShouldCreateVerificationToken()
    {
        var userRepository =
            new FakeUserRepository();

        var tokenRepository =
            new FakeEmailVerificationTokenRepository();

        var generator =
            new FakeVerificationTokenGenerator
            {
                Token =
                    "customer-facing-token"
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
            userRepository.AddedUser!.Id,
            tokenRepository.AddedToken.UserId);

        Assert.Equal(
            "hashed::customer-facing-token",
            tokenRepository.AddedToken.TokenHash);

        Assert.Equal(
            FixedUtcNow,
            tokenRepository.AddedToken.CreatedAtUtc);

        Assert.Equal(
            FixedUtcNow.AddHours(24),
            tokenRepository.AddedToken.ExpiresAtUtc);

        Assert.Equal(
            1,
            generator.GenerateCallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnRawVerificationToken()
    {
        var userRepository =
            new FakeUserRepository();

        var tokenRepository =
            new FakeEmailVerificationTokenRepository();

        var generator =
            new FakeVerificationTokenGenerator
            {
                Token =
                    "raw-email-token"
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

        Assert.Equal(
            "raw-email-token",
            result.Value.EmailVerificationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldStoreHashInsteadOfRawToken()
    {
        var userRepository =
            new FakeUserRepository();

        var tokenRepository =
            new FakeEmailVerificationTokenRepository();

        var generator =
            new FakeVerificationTokenGenerator
            {
                Token =
                    "raw-email-token"
            };

        var handler =
            CreateHandler(
                userRepository,
                tokenRepository,
                verificationTokenGenerator: generator);

        await handler.HandleAsync(
            new RegisterCommand(
                "user@example.com",
                "SecretPassword123!"));

        Assert.NotNull(
            tokenRepository.AddedToken);

        Assert.NotEqual(
            "raw-email-token",
            tokenRepository.AddedToken.TokenHash);

        Assert.Equal(
            "hashed::raw-email-token",
            tokenRepository.AddedToken.TokenHash);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailExists_ShouldNotCreateVerificationToken()
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

        var generator =
            new FakeVerificationTokenGenerator();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateHandler(
                userRepository,
                tokenRepository,
                unitOfWork: unitOfWork,
                verificationTokenGenerator: generator);

        var result =
            await handler.HandleAsync(
                new RegisterCommand(
                    "USER@example.com",
                    "SecretPassword123!"));

        Assert.True(result.IsFailure);

        Assert.Equal(
            RegistrationErrors.EmailAlreadyExists,
            result.Error);

        Assert.Null(
            tokenRepository.AddedToken);

        Assert.Equal(
            0,
            generator.GenerateCallCount);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_ShouldRaiseUserRegisteredEvent()
    {
        var userRepository =
            new FakeUserRepository();

        var tokenRepository =
            new FakeEmailVerificationTokenRepository();

        var handler =
            CreateHandler(
                userRepository,
                tokenRepository);

        var result =
            await handler.HandleAsync(
                new RegisterCommand(
                    "user@example.com",
                    "SecretPassword123!"));

        Assert.True(result.IsSuccess);

        var domainEvent =
            Assert.Single(
                userRepository.AddedUser!.DomainEvents);

        var registered =
            Assert.IsType<UserRegistered>(
                domainEvent);

        Assert.Equal(
            userRepository.AddedUser.Id,
            registered.UserId);

        Assert.Equal(
            FixedUtcNow,
            registered.RegisteredAtUtc);
    }

    private static RegisterCommandHandler CreateHandler(
        FakeUserRepository userRepository,
        FakeEmailVerificationTokenRepository tokenRepository,
        FakePasswordHasher? passwordHasher = null,
        FakeUnitOfWork? unitOfWork = null,
        FakeVerificationTokenGenerator? verificationTokenGenerator = null)
    {
        return new RegisterCommandHandler(
            userRepository,
            tokenRepository,
            passwordHasher ??
                new FakePasswordHasher(),
            verificationTokenGenerator ??
                new FakeVerificationTokenGenerator(),
            new FakeTokenHasher(),
            unitOfWork ??
                new FakeUnitOfWork(),
            new FakeClock(
                FixedUtcNow),
            VerificationOptions);
    }
}
