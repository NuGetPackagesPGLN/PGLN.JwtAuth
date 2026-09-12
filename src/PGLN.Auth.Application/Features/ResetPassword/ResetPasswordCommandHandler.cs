using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Features.ResetPassword;

public sealed class ResetPasswordCommandHandler
    : ICommandHandler<
        ResetPasswordCommand,
        Result>
{
    private const string RefreshTokenRevocationReason =
        "PasswordReset";

    private const string SessionRevocationReason =
        "Password reset.";

    private readonly IUserRepository _userRepository;

    private readonly IPasswordResetTokenRepository
        _passwordResetTokenRepository;

    private readonly IRefreshTokenRepository
        _refreshTokenRepository;

    private readonly IAuthSessionRepository
        _authSessionRepository;

    private readonly IPasswordHasher
        _passwordHasher;

    private readonly ITokenHasher
        _tokenHasher;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly IClock
        _clock;

    public ResetPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IAuthSessionRepository authSessionRepository,
        IPasswordHasher passwordHasher,
        ITokenHasher tokenHasher,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(
            userRepository);

        ArgumentNullException.ThrowIfNull(
            passwordResetTokenRepository);

        ArgumentNullException.ThrowIfNull(
            refreshTokenRepository);

        ArgumentNullException.ThrowIfNull(
            authSessionRepository);

        ArgumentNullException.ThrowIfNull(
            passwordHasher);

        ArgumentNullException.ThrowIfNull(
            tokenHasher);

        ArgumentNullException.ThrowIfNull(
            unitOfWork);

        ArgumentNullException.ThrowIfNull(
            clock);

        _userRepository =
            userRepository;

        _passwordResetTokenRepository =
            passwordResetTokenRepository;

        _refreshTokenRepository =
            refreshTokenRepository;

        _authSessionRepository =
            authSessionRepository;

        _passwordHasher =
            passwordHasher;

        _tokenHasher =
            tokenHasher;

        _unitOfWork =
            unitOfWork;

        _clock =
            clock;
    }

    public async Task<Result> HandleAsync(
        ResetPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        var now =
            _clock.UtcNow;

        var presentedTokenHash =
            _tokenHasher.Hash(
                command.ResetToken);

        var resetToken =
            await _passwordResetTokenRepository
                .GetByTokenHashAsync(
                    presentedTokenHash,
                    cancellationToken);

        if (resetToken is null)
        {
            return Result.Failure(
                ResetPasswordErrors.InvalidToken);
        }

        if (resetToken.IsUsed)
        {
            return Result.Failure(
                ResetPasswordErrors.UsedToken);
        }

        if (resetToken.IsExpired(
                now))
        {
            return Result.Failure(
                ResetPasswordErrors.ExpiredToken);
        }

        var email =
            Email.Create(
                command.Email);

        var user =
            await _userRepository
                .GetByNormalizedEmailAsync(
                    email.NormalizedValue,
                    cancellationToken);

        //
        // Do not expose whether the email exists or whether
        // the supplied token belongs to another account.
        //
        if (
            user is null ||
            resetToken.UserId != user.Id)
        {
            return Result.Failure(
                ResetPasswordErrors.InvalidRequest);
        }

        var newPasswordHash =
            _passwordHasher.Hash(
                command.NewPassword);

        user.ChangePassword(
            newPasswordHash,
            now);

        resetToken.MarkAsUsed(
            now);

        //
        // A password reset is a security boundary.
        //
        // Revoke every currently-active refresh token so that
        // previously authenticated sessions must log in again.
        //
        var refreshTokens =
            await _refreshTokenRepository
                .GetByUserIdAsync(
                    user.Id,
                    cancellationToken);

        foreach (var refreshToken in refreshTokens)
        {
            if (!refreshToken.IsActive(
                    now))
            {
                continue;
            }

            refreshToken.Revoke(
                now,
                RefreshTokenRevocationReason);
        }

        //
        // Keep authentication session state consistent with
        // refresh-token revocation. A password reset should
        // invalidate every authenticated device/session.
        //
        var sessions =
            await _authSessionRepository
                .GetByUserIdAsync(
                    user.Id,
                    cancellationToken);

        foreach (var session in sessions)
        {
            if (!session.IsActive)
            {
                continue;
            }

            session.Revoke(
                now,
                SessionRevocationReason);
        }

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        return Result.Success();
    }
}

