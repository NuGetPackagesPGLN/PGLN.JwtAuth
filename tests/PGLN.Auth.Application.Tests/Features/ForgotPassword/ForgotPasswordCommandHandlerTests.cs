using PGLN.Auth.Application.Events.Email;
using PGLN.Auth.Application.Features.ForgotPassword;
using PGLN.Auth.Application.Tests.TestDoubles;
using PGLN.Auth.Domain.PasswordResets;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Tests.Features.ForgotPassword;

public sealed class ForgotPasswordCommandHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            8,
            18,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_WithKnownEmail_ShouldCreateResetToken()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var tokens =
            new FakePasswordResetTokenRepository();

        var generator =
            new FakePasswordResetTokenGenerator
            {
                Token =
                    "raw-reset-token"
            };

        var publisher =
            new FakeIntegrationEventPublisher();

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    users,
                    tokens,
                    generator,
                    publisher,
                    unitOfWork)
                .HandleAsync(
                    new ForgotPasswordCommand(
                        user.Email.Value));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            1,
            generator.GenerateCallCount);

        Assert.Equal(
            1,
            tokens.AddCallCount);

        var token =
            Assert.Single(
                tokens.Tokens);

        Assert.Equal(
            user.Id,
            token.UserId);

        Assert.Equal(
            "hashed::raw-reset-token",
            token.TokenHash);

        Assert.Equal(
            Now,
            token.CreatedAtUtc);

        Assert.Equal(
            Now.AddMinutes(30),
            token.ExpiresAtUtc);

        Assert.False(
            token.IsUsed);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldStoreHashAndNotRawToken()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var tokens =
            new FakePasswordResetTokenRepository();

        var generator =
            new FakePasswordResetTokenGenerator
            {
                Token =
                    "super-secret-reset-token"
            };

        var result =
            await CreateHandler(
                    users,
                    tokens,
                    generator)
                .HandleAsync(
                    new ForgotPasswordCommand(
                        user.Email.Value));

        Assert.True(
            result.IsSuccess);

        var token =
            Assert.Single(
                tokens.Tokens);

        Assert.Equal(
            "hashed::super-secret-reset-token",
            token.TokenHash);

        Assert.NotEqual(
            "super-secret-reset-token",
            token.TokenHash);
    }

    [Fact]
    public async Task HandleAsync_WithKnownEmail_ShouldPublishPasswordResetRequested()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var generator =
            new FakePasswordResetTokenGenerator
            {
                Token =
                    "raw-reset-token"
            };

        var publisher =
            new FakeIntegrationEventPublisher();

        var result =
            await CreateHandler(
                    users,
                    passwordResetTokenGenerator:
                        generator,
                    integrationEventPublisher:
                        publisher)
                .HandleAsync(
                    new ForgotPasswordCommand(
                        user.Email.Value));

        Assert.True(
            result.IsSuccess);

        var integrationEvent =
            Assert.IsType<PasswordResetRequested>(
                Assert.Single(
                    publisher.Events));

        Assert.Equal(
            user.Id,
            integrationEvent.UserId);

        Assert.Equal(
            user.Email.Value,
            integrationEvent.Email);

        Assert.Equal(
            "raw-reset-token",
            integrationEvent.ResetToken);

        Assert.Equal(
            Now,
            integrationEvent.OccurredAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownEmail_ShouldReturnSuccess()
    {
        var users =
            new FakeUserRepository();

        var tokens =
            new FakePasswordResetTokenRepository();

        var generator =
            new FakePasswordResetTokenGenerator();

        var publisher =
            new FakeIntegrationEventPublisher();

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    users,
                    tokens,
                    generator,
                    publisher,
                    unitOfWork)
                .HandleAsync(
                    new ForgotPasswordCommand(
                        "missing@example.com"));

        Assert.True(
            result.IsSuccess);

        Assert.Empty(
            tokens.Tokens);

        Assert.Equal(
            0,
            tokens.AddCallCount);

        Assert.Equal(
            0,
            generator.GenerateCallCount);

        Assert.Empty(
            publisher.Events);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithDifferentEmailCase_ShouldFindUser()
    {
        var user =
            CreateUser(
                "User@Example.com");

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var tokens =
            new FakePasswordResetTokenRepository();

        var result =
            await CreateHandler(
                    users,
                    tokens)
                .HandleAsync(
                    new ForgotPasswordCommand(
                        "USER@example.com"));

        Assert.True(
            result.IsSuccess);

        Assert.Single(
            tokens.Tokens);
    }

    [Fact]
    public async Task HandleAsync_WithExistingActiveToken_ShouldInvalidateExistingToken()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var tokens =
            new FakePasswordResetTokenRepository();

        var existing =
            PasswordResetToken.Create(
                PasswordResetTokenId.New(),
                user.Id,
                "hashed::old-reset-token",
                Now.AddMinutes(-10),
                Now.AddMinutes(20));

        tokens.Seed(
            existing);

        var result =
            await CreateHandler(
                    users,
                    tokens)
                .HandleAsync(
                    new ForgotPasswordCommand(
                        user.Email.Value));

        Assert.True(
            result.IsSuccess);

        Assert.True(
            existing.IsUsed);

        Assert.Equal(
            Now,
            existing.UsedAtUtc);

        Assert.Equal(
            2,
            tokens.Tokens.Count);

        var replacement =
            tokens.Tokens.Single(
                token =>
                    token.Id != existing.Id);

        Assert.True(
            replacement.CanBeUsed(
                Now));
    }

    [Fact]
    public async Task HandleAsync_WithExpiredExistingToken_ShouldNotModifyIt()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var tokens =
            new FakePasswordResetTokenRepository();

        var expired =
            PasswordResetToken.Create(
                PasswordResetTokenId.New(),
                user.Id,
                "hashed::expired",
                Now.AddHours(-1),
                Now.AddMinutes(-1));

        tokens.Seed(
            expired);

        var result =
            await CreateHandler(
                    users,
                    tokens)
                .HandleAsync(
                    new ForgotPasswordCommand(
                        user.Email.Value));

        Assert.True(
            result.IsSuccess);

        Assert.False(
            expired.IsUsed);

        Assert.Equal(
            2,
            tokens.Tokens.Count);
    }

    [Fact]
    public async Task HandleAsync_ShouldUseConfiguredLifetime()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var tokens =
            new FakePasswordResetTokenRepository();

        var options =
            new PasswordResetOptions
            {
                TokenLifetime =
                    TimeSpan.FromHours(2)
            };

        var result =
            await CreateHandler(
                    users,
                    tokens,
                    options:
                        options)
                .HandleAsync(
                    new ForgotPasswordCommand(
                        user.Email.Value));

        Assert.True(
            result.IsSuccess);

        var token =
            Assert.Single(
                tokens.Tokens);

        Assert.Equal(
            Now.AddHours(2),
            token.ExpiresAtUtc);
    }

    [Fact]
    public async Task HandleAsync_ShouldSaveChangesExactlyOnce()
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
                    new ForgotPasswordCommand(
                        user.Email.Value));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    private static ForgotPasswordCommandHandler CreateHandler(
        FakeUserRepository userRepository,
        FakePasswordResetTokenRepository? passwordResetTokenRepository = null,
        FakePasswordResetTokenGenerator? passwordResetTokenGenerator = null,
        FakeIntegrationEventPublisher? integrationEventPublisher = null,
        FakeUnitOfWork? unitOfWork = null,
        PasswordResetOptions? options = null)
    {
        return new ForgotPasswordCommandHandler(
            userRepository,
            passwordResetTokenRepository ??
                new FakePasswordResetTokenRepository(),
            passwordResetTokenGenerator ??
                new FakePasswordResetTokenGenerator(),
            new FakeTokenHasher(),
            integrationEventPublisher ??
                new FakeIntegrationEventPublisher(),
            unitOfWork ??
                new FakeUnitOfWork(),
            new FakeClock(
                Now),
            options ??
                new PasswordResetOptions());
    }

    private static User CreateUser(
        string email =
            "user@example.com")
    {
        var user =
            User.Register(
                UserId.New(),
                Email.Create(
                    email),
                "hashed-password",
                Now.AddDays(-1));

        user.ClearDomainEvents();

        return user;
    }
}
