using PGLN.Auth.IntegrationTests.TestHelpers;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Domain.PasswordResets;
using PGLN.Auth.Domain.RefreshTokens;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.IntegrationTests.Http;

public sealed class ResetPasswordEndpointHttpTests
{
    [Fact]
    public async Task ResetPassword_WithValidRequest_ChangesPasswordAndConsumesToken()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        const string email =
            "reset-password@example.com";

        const string oldPassword =
            "OldPassword123!";

        const string newPassword =
            "NewPassword456!";

        const string rawResetToken =
            "valid-password-reset-token";

        UserId userId;

        PasswordResetTokenId resetTokenId;

        await using (
            var scope =
                application.Application.Services
                    .CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var passwordHasher =
                scope.ServiceProvider
                    .GetRequiredService<IPasswordHasher>();

            var tokenHasher =
                scope.ServiceProvider
                    .GetRequiredService<ITokenHasher>();

            var now =
                DateTimeOffset.UtcNow;

            var user =
                User.Register(
                    UserId.New(),
                    Email.Create(
                        email),
                    passwordHasher.Hash(
                        oldPassword),
                    now.AddDays(-1));

            user.ClearDomainEvents();

            userId =
                user.Id;

            var resetToken =
                PasswordResetToken.Create(
                    PasswordResetTokenId.New(),
                    user.Id,
                    tokenHasher.Hash(
                        rawResetToken),
                    now.AddMinutes(-5),
                    now.AddMinutes(30));

            resetTokenId =
                resetToken.Id;

            dbContext.Users.Add(
                user);

            dbContext.PasswordResetTokens.Add(
                resetToken);

            await dbContext.SaveChangesAsync();
        }

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/reset-password",
                new
                {
                    Email =
                        email,

                    Token =
                        rawResetToken,

                    NewPassword =
                        newPassword
                });

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using (
            var scope =
                application.Application.Services
                    .CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var passwordHasher =
                scope.ServiceProvider
                    .GetRequiredService<IPasswordHasher>();

            var persistedUser =
                await dbContext.Users
                    .SingleAsync(
                        user =>
                            user.Id == userId);

            var persistedResetToken =
                await dbContext.PasswordResetTokens
                    .SingleAsync(
                        token =>
                            token.Id == resetTokenId);

            Assert.True(
                passwordHasher.Verify(
                    newPassword,
                    persistedUser.PasswordHash!));

            Assert.False(
                passwordHasher.Verify(
                    oldPassword,
                    persistedUser.PasswordHash!));

            Assert.True(
                persistedResetToken.IsUsed);

            Assert.NotNull(
                persistedResetToken.UsedAtUtc);
        }
    }


    [Fact]
    public async Task ResetPassword_WithInvalidToken_ShouldReturn400()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        const string email =
            "invalid-token@example.com";

        const string oldPassword =
            "OldPassword123!";

        await using (
            var scope =
                application.Application.Services
                    .CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var passwordHasher =
                scope.ServiceProvider
                    .GetRequiredService<IPasswordHasher>();

            var now =
                DateTimeOffset.UtcNow;

            var user =
                User.Register(
                    UserId.New(),
                    Email.Create(
                        email),
                    passwordHasher.Hash(
                        oldPassword),
                    now.AddDays(-1));

            user.ClearDomainEvents();

            dbContext.Users.Add(
                user);

            await dbContext.SaveChangesAsync();
        }

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/reset-password",
                new
                {
                    Email =
                        email,

                    Token =
                        "this-token-does-not-exist",

                    NewPassword =
                        "NewPassword456!"
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var body =
            await response.Content
                .ReadAsStringAsync();

        Assert.Contains(
            "ResetPassword.InvalidToken",
            body);
    }


    [Fact]
    public async Task ResetPassword_WithExpiredToken_ShouldReturn410()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        const string email =
            "expired-reset-token@example.com";

        const string oldPassword =
            "OldPassword123!";

        const string newPassword =
            "NewPassword456!";

        const string rawResetToken =
            "expired-password-reset-token";

        UserId userId;

        PasswordResetTokenId resetTokenId;

        await using (
            var scope =
                application.Application.Services
                    .CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var passwordHasher =
                scope.ServiceProvider
                    .GetRequiredService<IPasswordHasher>();

            var tokenHasher =
                scope.ServiceProvider
                    .GetRequiredService<ITokenHasher>();

            var now =
                DateTimeOffset.UtcNow;

            var user =
                User.Register(
                    UserId.New(),
                    Email.Create(
                        email),
                    passwordHasher.Hash(
                        oldPassword),
                    now.AddDays(-1));

            user.ClearDomainEvents();

            userId =
                user.Id;

            var resetToken =
                PasswordResetToken.Create(
                    PasswordResetTokenId.New(),
                    user.Id,
                    tokenHasher.Hash(
                        rawResetToken),
                    now.AddHours(-1),
                    now.AddMinutes(-30));

            resetTokenId =
                resetToken.Id;

            dbContext.Users.Add(
                user);

            dbContext.PasswordResetTokens.Add(
                resetToken);

            await dbContext.SaveChangesAsync();
        }

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/reset-password",
                new
                {
                    Email =
                        email,

                    Token =
                        rawResetToken,

                    NewPassword =
                        newPassword
                });

        Assert.Equal(
            HttpStatusCode.Gone,
            response.StatusCode);

        var body =
            await response.Content
                .ReadAsStringAsync();

        Assert.Contains(
            "ResetPassword.ExpiredToken",
            body);

        await using (
            var scope =
                application.Application.Services
                    .CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var passwordHasher =
                scope.ServiceProvider
                    .GetRequiredService<IPasswordHasher>();

            var persistedUser =
                await dbContext.Users
                    .SingleAsync(
                        user =>
                            user.Id == userId);

            var persistedResetToken =
                await dbContext.PasswordResetTokens
                    .SingleAsync(
                        token =>
                            token.Id == resetTokenId);

            Assert.True(
                passwordHasher.Verify(
                    oldPassword,
                    persistedUser.PasswordHash!));

            Assert.False(
                passwordHasher.Verify(
                    newPassword,
                    persistedUser.PasswordHash!));

            Assert.False(
                persistedResetToken.IsUsed);

            Assert.Null(
                persistedResetToken.UsedAtUtc);
        }
    }


    [Fact]
    public async Task ResetPassword_WithUsedToken_ShouldReturn409()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        const string email =
            "used-reset-token@example.com";

        const string oldPassword =
            "OldPassword123!";

        const string firstNewPassword =
            "FirstNewPassword456!";

        const string replayPassword =
            "ReplayPassword789!";

        const string rawResetToken =
            "single-use-password-reset-token";

        UserId userId;

        PasswordResetTokenId resetTokenId;

        await using (
            var scope =
                application.Application.Services
                    .CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var passwordHasher =
                scope.ServiceProvider
                    .GetRequiredService<IPasswordHasher>();

            var tokenHasher =
                scope.ServiceProvider
                    .GetRequiredService<ITokenHasher>();

            var now =
                DateTimeOffset.UtcNow;

            var user =
                User.Register(
                    UserId.New(),
                    Email.Create(
                        email),
                    passwordHasher.Hash(
                        oldPassword),
                    now.AddDays(-1));

            user.ClearDomainEvents();

            userId =
                user.Id;

            var resetToken =
                PasswordResetToken.Create(
                    PasswordResetTokenId.New(),
                    user.Id,
                    tokenHasher.Hash(
                        rawResetToken),
                    now.AddMinutes(-5),
                    now.AddMinutes(30));

            resetTokenId =
                resetToken.Id;

            dbContext.Users.Add(
                user);

            dbContext.PasswordResetTokens.Add(
                resetToken);

            await dbContext.SaveChangesAsync();
        }


        // First use succeeds.
        var firstResponse =
            await client.PostAsJsonAsync(
                "/api/auth/reset-password",
                new
                {
                    Email =
                        email,

                    Token =
                        rawResetToken,

                    NewPassword =
                        firstNewPassword
                });

        Assert.Equal(
            HttpStatusCode.NoContent,
            firstResponse.StatusCode);


        // Attempt to replay the exact same reset token.
        var replayResponse =
            await client.PostAsJsonAsync(
                "/api/auth/reset-password",
                new
                {
                    Email =
                        email,

                    Token =
                        rawResetToken,

                    NewPassword =
                        replayPassword
                });

        Assert.Equal(
            HttpStatusCode.Conflict,
            replayResponse.StatusCode);

        var body =
            await replayResponse.Content
                .ReadAsStringAsync();

        Assert.Contains(
            "ResetPassword.UsedToken",
            body);


        // Verify the replay changed nothing.
        await using (
            var scope =
                application.Application.Services
                    .CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var passwordHasher =
                scope.ServiceProvider
                    .GetRequiredService<IPasswordHasher>();

            var persistedUser =
                await dbContext.Users
                    .SingleAsync(
                        user =>
                            user.Id == userId);

            var persistedResetToken =
                await dbContext.PasswordResetTokens
                    .SingleAsync(
                        token =>
                            token.Id == resetTokenId);

            Assert.True(
                passwordHasher.Verify(
                    firstNewPassword,
                    persistedUser.PasswordHash!));

            Assert.False(
                passwordHasher.Verify(
                    replayPassword,
                    persistedUser.PasswordHash!));

            Assert.False(
                passwordHasher.Verify(
                    oldPassword,
                    persistedUser.PasswordHash!));

            Assert.True(
                persistedResetToken.IsUsed);

            Assert.NotNull(
                persistedResetToken.UsedAtUtc);
        }
    }

    [Fact]
    public async Task ResetPassword_WithTokenBelongingToAnotherUser_ShouldReturn400()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        const string tokenOwnerEmail =
            "reset-owner@example.com";

        const string requestEmail =
            "different-user@example.com";

        const string ownerPassword =
            "OwnerPassword123!";

        const string otherUserPassword =
            "OtherPassword123!";

        const string attemptedNewPassword =
            "AttemptedPassword456!";

        const string rawResetToken =
            "token-owned-by-first-user";

        UserId tokenOwnerId;

        UserId requestUserId;

        PasswordResetTokenId resetTokenId;

        await using (
            var scope =
                application.Application.Services
                    .CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var passwordHasher =
                scope.ServiceProvider
                    .GetRequiredService<IPasswordHasher>();

            var tokenHasher =
                scope.ServiceProvider
                    .GetRequiredService<ITokenHasher>();

            var now =
                DateTimeOffset.UtcNow;


            // ------------------------------------------------
            // User A owns the reset token.
            // ------------------------------------------------

            var tokenOwner =
                User.Register(
                    UserId.New(),
                    Email.Create(
                        tokenOwnerEmail),
                    passwordHasher.Hash(
                        ownerPassword),
                    now.AddDays(-2));

            tokenOwner.ClearDomainEvents();

            tokenOwnerId =
                tokenOwner.Id;


            // ------------------------------------------------
            // User B is supplied in the HTTP request.
            // ------------------------------------------------

            var requestUser =
                User.Register(
                    UserId.New(),
                    Email.Create(
                        requestEmail),
                    passwordHasher.Hash(
                        otherUserPassword),
                    now.AddDays(-1));

            requestUser.ClearDomainEvents();

            requestUserId =
                requestUser.Id;


            // ------------------------------------------------
            // Reset token belongs ONLY to User A.
            // ------------------------------------------------

            var resetToken =
                PasswordResetToken.Create(
                    PasswordResetTokenId.New(),
                    tokenOwner.Id,
                    tokenHasher.Hash(
                        rawResetToken),
                    now.AddMinutes(-5),
                    now.AddMinutes(30));

            resetTokenId =
                resetToken.Id;


            dbContext.Users.Add(
                tokenOwner);

            dbContext.Users.Add(
                requestUser);

            dbContext.PasswordResetTokens.Add(
                resetToken);

            await dbContext.SaveChangesAsync();
        }


        // ====================================================
        // Act
        // Supply User B's email with User A's valid token.
        // ====================================================

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/reset-password",
                new
                {
                    Email =
                        requestEmail,

                    Token =
                        rawResetToken,

                    NewPassword =
                        attemptedNewPassword
                });


        // ====================================================
        // Assert HTTP response
        // ====================================================

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var body =
            await response.Content
                .ReadAsStringAsync();

        Assert.Contains(
            "ResetPassword.InvalidRequest",
            body);


        // ====================================================
        // Assert database state was not changed
        // ====================================================

        await using (
            var scope =
                application.Application.Services
                    .CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var passwordHasher =
                scope.ServiceProvider
                    .GetRequiredService<IPasswordHasher>();

            var persistedTokenOwner =
                await dbContext.Users
                    .SingleAsync(
                        user =>
                            user.Id == tokenOwnerId);

            var persistedRequestUser =
                await dbContext.Users
                    .SingleAsync(
                        user =>
                            user.Id == requestUserId);

            var persistedResetToken =
                await dbContext.PasswordResetTokens
                    .SingleAsync(
                        token =>
                            token.Id == resetTokenId);


            // User A's password must remain unchanged.
            Assert.True(
                passwordHasher.Verify(
                    ownerPassword,
                    persistedTokenOwner.PasswordHash!));

            Assert.False(
                passwordHasher.Verify(
                    attemptedNewPassword,
                    persistedTokenOwner.PasswordHash!));


            // User B's password must also remain unchanged.
            Assert.True(
                passwordHasher.Verify(
                    otherUserPassword,
                    persistedRequestUser.PasswordHash!));

            Assert.False(
                passwordHasher.Verify(
                    attemptedNewPassword,
                    persistedRequestUser.PasswordHash!));


            // The mismatched request must not consume the token.
            Assert.False(
                persistedResetToken.IsUsed);

            Assert.Null(
                persistedResetToken.UsedAtUtc);
        }
    }

    [Fact]
    public async Task ResetPassword_WithInvalidInput_ShouldReturn400WithoutConsumingToken()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        const string email =
            "validation-reset@example.com";

        const string oldPassword =
            "OldPassword123!";

        const string attemptedNewPassword =
            "NewPassword456!";

        const string rawResetToken =
            "valid-token-for-validation-tests";

        UserId userId;

        PasswordResetTokenId resetTokenId;

        await using (
            var scope =
                application.Application.Services
                    .CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var passwordHasher =
                scope.ServiceProvider
                    .GetRequiredService<IPasswordHasher>();

            var tokenHasher =
                scope.ServiceProvider
                    .GetRequiredService<ITokenHasher>();

            var now =
                DateTimeOffset.UtcNow;

            var user =
                User.Register(
                    UserId.New(),
                    Email.Create(email),
                    passwordHasher.Hash(oldPassword),
                    now.AddDays(-1));

            user.ClearDomainEvents();

            userId =
                user.Id;

            var resetToken =
                PasswordResetToken.Create(
                    PasswordResetTokenId.New(),
                    user.Id,
                    tokenHasher.Hash(rawResetToken),
                    now.AddMinutes(-5),
                    now.AddMinutes(30));

            resetTokenId =
                resetToken.Id;

            dbContext.Users.Add(user);

            dbContext.PasswordResetTokens.Add(
                resetToken);

            await dbContext.SaveChangesAsync();
        }


        // Invalid email
        var invalidEmailResponse =
            await client.PostAsJsonAsync(
                "/api/auth/reset-password",
                new
                {
                    Email = "not-an-email",
                    Token = rawResetToken,
                    NewPassword = attemptedNewPassword
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            invalidEmailResponse.StatusCode);


        // Empty token
        var emptyTokenResponse =
            await client.PostAsJsonAsync(
                "/api/auth/reset-password",
                new
                {
                    Email = email,
                    Token = string.Empty,
                    NewPassword = attemptedNewPassword
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            emptyTokenResponse.StatusCode);


        // Weak password
        var weakPasswordResponse =
            await client.PostAsJsonAsync(
                "/api/auth/reset-password",
                new
                {
                    Email = email,
                    Token = rawResetToken,
                    NewPassword = "weak"
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            weakPasswordResponse.StatusCode);


        // Verify validation caused no state changes.
        await using (
            var scope =
                application.Application.Services
                    .CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var passwordHasher =
                scope.ServiceProvider
                    .GetRequiredService<IPasswordHasher>();

            var persistedUser =
                await dbContext.Users
                    .SingleAsync(
                        user =>
                            user.Id == userId);

            var persistedResetToken =
                await dbContext.PasswordResetTokens
                    .SingleAsync(
                        token =>
                            token.Id == resetTokenId);

            Assert.True(
                passwordHasher.Verify(
                    oldPassword,
                    persistedUser.PasswordHash!));

            Assert.False(
                passwordHasher.Verify(
                    attemptedNewPassword,
                    persistedUser.PasswordHash!));

            Assert.False(
                persistedResetToken.IsUsed);

            Assert.Null(
                persistedResetToken.UsedAtUtc);
        }
    }

    [Fact]
    public async Task ResetPassword_WithActiveRefreshTokens_ShouldRevokeExistingSessions()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        const string email =
            "session-reset@example.com";

        const string oldPassword =
            "OldPassword123!";

        const string newPassword =
            "NewPassword456!";

        const string rawResetToken =
            "session-reset-token";

        UserId userId;

        PasswordResetTokenId resetTokenId;

        RefreshTokenId activeTokenAId;

        RefreshTokenId activeTokenBId;

        RefreshTokenId alreadyRevokedTokenId;

        await using (
            var scope =
                application.Application.Services
                    .CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var passwordHasher =
                scope.ServiceProvider
                    .GetRequiredService<IPasswordHasher>();

            var tokenHasher =
                scope.ServiceProvider
                    .GetRequiredService<ITokenHasher>();

            var now =
                DateTimeOffset.UtcNow;


            // ------------------------------------------------
            // User
            // ------------------------------------------------

            var user =
                User.Register(
                    UserId.New(),
                    Email.Create(email),
                    passwordHasher.Hash(oldPassword),
                    now.AddDays(-10));

            user.ClearDomainEvents();

            userId =
                user.Id;


            // ------------------------------------------------
            // Password reset token
            // ------------------------------------------------

            var resetToken =
                PasswordResetToken.Create(
                    PasswordResetTokenId.New(),
                    user.Id,
                    tokenHasher.Hash(rawResetToken),
                    now.AddMinutes(-5),
                    now.AddMinutes(30));

            resetTokenId =
                resetToken.Id;


            // ------------------------------------------------
            // Active refresh token A
            // ------------------------------------------------

            var session =
                AuthSessionTestFactory.Create(
                    user.Id,
                    now.AddDays(-20));

            dbContext.AuthSessions.Add(
                session);

            var activeTokenA =
                RefreshToken.Create(
                    RefreshTokenId.New(),
                    RefreshTokenFamilyId.New(),
                    user.Id,
                    session.Id,
                    tokenHasher.Hash(
                        "active-refresh-token-a"),
                    now.AddDays(-5),
                    now.AddDays(25));

            activeTokenAId =
                activeTokenA.Id;


            // ------------------------------------------------
            // Active refresh token B
            // ------------------------------------------------

            var activeTokenB =
                RefreshToken.Create(
                    RefreshTokenId.New(),
                    RefreshTokenFamilyId.New(),
                    user.Id,
                    session.Id,
                    tokenHasher.Hash(
                        "active-refresh-token-b"),
                    now.AddDays(-2),
                    now.AddDays(28));

            activeTokenBId =
                activeTokenB.Id;


            // ------------------------------------------------
            // Token already revoked before password reset
            // ------------------------------------------------

            var alreadyRevokedToken =
                RefreshToken.Create(
                    RefreshTokenId.New(),
                    RefreshTokenFamilyId.New(),
                    user.Id,
                    session.Id,
                    tokenHasher.Hash(
                        "already-revoked-refresh-token"),
                    now.AddDays(-20),
                    now.AddDays(10));

            alreadyRevokedToken.Revoke(
                now.AddDays(-1),
                "UserLogout");

            alreadyRevokedTokenId =
                alreadyRevokedToken.Id;


            // ------------------------------------------------
            // Persist test state
            // ------------------------------------------------

            dbContext.Users.Add(
                user);

            dbContext.PasswordResetTokens.Add(
                resetToken);

            dbContext.RefreshTokens.AddRange(
                activeTokenA,
                activeTokenB,
                alreadyRevokedToken);

            await dbContext.SaveChangesAsync();
        }


        // ====================================================
        // Perform password reset
        // ====================================================

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/reset-password",
                new
                {
                    Email = email,
                    Token = rawResetToken,
                    NewPassword = newPassword
                });

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);


        // ====================================================
        // Reload persisted state
        // ====================================================

        await using (
            var scope =
                application.Application.Services
                    .CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var passwordHasher =
                scope.ServiceProvider
                    .GetRequiredService<IPasswordHasher>();


            var persistedUser =
                await dbContext.Users
                    .SingleAsync(
                        user =>
                            user.Id == userId);


            var persistedResetToken =
                await dbContext.PasswordResetTokens
                    .SingleAsync(
                        token =>
                            token.Id == resetTokenId);


            var persistedActiveTokenA =
                await dbContext.RefreshTokens
                    .SingleAsync(
                        token =>
                            token.Id == activeTokenAId);


            var persistedActiveTokenB =
                await dbContext.RefreshTokens
                    .SingleAsync(
                        token =>
                            token.Id == activeTokenBId);


            var persistedAlreadyRevokedToken =
                await dbContext.RefreshTokens
                    .SingleAsync(
                        token =>
                            token.Id == alreadyRevokedTokenId);


            // ------------------------------------------------
            // Password changed
            // ------------------------------------------------

            Assert.False(
                passwordHasher.Verify(
                    oldPassword,
                    persistedUser.PasswordHash!));

            Assert.True(
                passwordHasher.Verify(
                    newPassword,
                    persistedUser.PasswordHash!));


            // ------------------------------------------------
            // Reset token consumed
            // ------------------------------------------------

            Assert.True(
                persistedResetToken.IsUsed);

            Assert.NotNull(
                persistedResetToken.UsedAtUtc);


            // ------------------------------------------------
            // Active session A revoked
            // ------------------------------------------------

            Assert.True(
                persistedActiveTokenA.IsRevoked);

            Assert.Equal(
                "PasswordReset",
                persistedActiveTokenA.RevocationReason);

            Assert.NotNull(
                persistedActiveTokenA.RevokedAtUtc);


            // ------------------------------------------------
            // Active session B revoked
            // ------------------------------------------------

            Assert.True(
                persistedActiveTokenB.IsRevoked);

            Assert.Equal(
                "PasswordReset",
                persistedActiveTokenB.RevocationReason);

            Assert.NotNull(
                persistedActiveTokenB.RevokedAtUtc);


            // ------------------------------------------------
            // Previously revoked session must not be rewritten
            // ------------------------------------------------

            Assert.True(
                persistedAlreadyRevokedToken.IsRevoked);

            Assert.Equal(
                "UserLogout",
                persistedAlreadyRevokedToken.RevocationReason);

            Assert.NotNull(
                persistedAlreadyRevokedToken.RevokedAtUtc);
        }
    }
}



