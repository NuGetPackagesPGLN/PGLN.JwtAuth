using PGLN.Auth.Application.Features.Registration;
using PGLN.Auth.Application.Tests.TestDoubles;
using PGLN.Auth.Domain.Users;
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

    [Fact]
    public async Task HandleAsync_WithValidCommand_ShouldRegisterUser()
    {
        var userRepository =
            new FakeUserRepository();

        var passwordHasher =
            new FakePasswordHasher
            {
                HashResult = "hashed-secret-password"
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var clock =
            new FakeClock(FixedUtcNow);

        var handler =
            new RegisterCommandHandler(
                userRepository,
                passwordHasher,
                unitOfWork,
                clock);

        var command =
            new RegisterCommand(
                "user@example.com",
                "Secret123!");

        var result =
            await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);

        Assert.NotNull(
            userRepository.AddedUser);

        var user =
            userRepository.AddedUser;

        Assert.Equal(
            "user@example.com",
            user.Email.Value);

        Assert.Equal(
            "USER@EXAMPLE.COM",
            user.Email.NormalizedValue);

        Assert.Equal(
            "hashed-secret-password",
            user.PasswordHash);

        Assert.False(
            user.EmailConfirmed);

        Assert.Equal(
            FixedUtcNow,
            user.CreatedAtUtc);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_ShouldHashPassword()
    {
        var userRepository =
            new FakeUserRepository();

        var passwordHasher =
            new FakePasswordHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var clock =
            new FakeClock(FixedUtcNow);

        var handler =
            new RegisterCommandHandler(
                userRepository,
                passwordHasher,
                unitOfWork,
                clock);

        var command =
            new RegisterCommand(
                "user@example.com",
                "Secret123!");

        var result =
            await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);

        Assert.Equal(
            "Secret123!",
            passwordHasher.LastPasswordHashed);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailAlreadyExists_ShouldReturnFailure()
    {
        var userRepository =
            new FakeUserRepository();

        var existingUser =
            User.Register(
                UserId.New(),
                Email.Create(
                    "user@example.com"),
                "existing-password-hash",
                FixedUtcNow);

        existingUser.ClearDomainEvents();

        userRepository.Seed(
            existingUser);

        var passwordHasher =
            new FakePasswordHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var clock =
            new FakeClock(FixedUtcNow);

        var handler =
            new RegisterCommandHandler(
                userRepository,
                passwordHasher,
                unitOfWork,
                clock);

        var command =
            new RegisterCommand(
                "USER@example.com",
                "Secret123!");

        var result =
            await handler.HandleAsync(command);

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            RegistrationErrors.EmailAlreadyExists,
            result.Error);

        Assert.Null(
            userRepository.AddedUser);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);

        Assert.Null(
            passwordHasher.LastPasswordHashed);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_ShouldReturnRegistrationResult()
    {
        var userRepository =
            new FakeUserRepository();

        var passwordHasher =
            new FakePasswordHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var clock =
            new FakeClock(FixedUtcNow);

        var handler =
            new RegisterCommandHandler(
                userRepository,
                passwordHasher,
                unitOfWork,
                clock);

        var command =
            new RegisterCommand(
                "user@example.com",
                "Secret123!");

        var result =
            await handler.HandleAsync(command);

        Assert.True(
            result.IsSuccess);

        Assert.NotEqual(
            Guid.Empty,
            result.Value.UserId.Value);

        Assert.Equal(
            "user@example.com",
            result.Value.Email);

        Assert.False(
            result.Value.EmailConfirmed);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_ShouldRaiseUserRegisteredEvent()
    {
        var userRepository =
            new FakeUserRepository();

        var passwordHasher =
            new FakePasswordHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var clock =
            new FakeClock(FixedUtcNow);

        var handler =
            new RegisterCommandHandler(
                userRepository,
                passwordHasher,
                unitOfWork,
                clock);

        var command =
            new RegisterCommand(
                "user@example.com",
                "Secret123!");

        var result =
            await handler.HandleAsync(command);

        Assert.True(
            result.IsSuccess);

        Assert.NotNull(
            userRepository.AddedUser);

        var domainEvent =
            Assert.Single(
                userRepository.AddedUser.DomainEvents);

        var registeredEvent =
            Assert.IsType<UserRegistered>(
                domainEvent);

        Assert.Equal(
            userRepository.AddedUser.Id,
            registeredEvent.UserId);

        Assert.Equal(
            "user@example.com",
            registeredEvent.Email);

        Assert.Equal(
            FixedUtcNow,
            registeredEvent.RegisteredAtUtc);
    }

    [Fact]
    public async Task HandleAsync_ShouldUseNormalizedEmailForDuplicateDetection()
    {
        var userRepository =
            new FakeUserRepository();

        var existingUser =
            User.Register(
                UserId.New(),
                Email.Create(
                    "User@Example.com"),
                "existing-password-hash",
                FixedUtcNow);

        existingUser.ClearDomainEvents();

        userRepository.Seed(
            existingUser);

        var passwordHasher =
            new FakePasswordHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var clock =
            new FakeClock(FixedUtcNow);

        var handler =
            new RegisterCommandHandler(
                userRepository,
                passwordHasher,
                unitOfWork,
                clock);

        var command =
            new RegisterCommand(
                "user@example.com",
                "Secret123!");

        var result =
            await handler.HandleAsync(command);

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            RegistrationErrors.EmailAlreadyExists,
            result.Error);
    }
}
