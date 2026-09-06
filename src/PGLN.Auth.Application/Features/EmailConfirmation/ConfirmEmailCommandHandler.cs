using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.EmailConfirmation;

public sealed class ConfirmEmailCommandHandler
    : ICommandHandler<
        ConfirmEmailCommand,
        Result<ConfirmEmailResult>>
{
    private readonly IEmailVerificationTokenRepository _tokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITokenHasher _tokenHasher;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmEmailCommandHandler(
        IEmailVerificationTokenRepository tokenRepository,
        IUserRepository userRepository,
        ITokenHasher tokenHasher,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        _tokenRepository = tokenRepository;
        _userRepository = userRepository;
        _tokenHasher = tokenHasher;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ConfirmEmailResult>> HandleAsync(
        ConfirmEmailCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tokenHash =
            _tokenHasher.Hash(command.Token);

        var token =
            await _tokenRepository.GetByTokenHashAsync(
                tokenHash,
                cancellationToken);

        if (token is null)
        {
            return Result<ConfirmEmailResult>.Failure(
                EmailConfirmationErrors.InvalidToken);
        }

        if (token.IsUsed)
        {
            return Result<ConfirmEmailResult>.Failure(
                EmailConfirmationErrors.TokenAlreadyUsed);
        }

        if (token.IsExpired(_clock.UtcNow))
        {
            return Result<ConfirmEmailResult>.Failure(
                EmailConfirmationErrors.ExpiredToken);
        }

        var user =
            await _userRepository.GetByIdAsync(
                token.UserId,
                cancellationToken);

        if (user is null)
        {
            return Result<ConfirmEmailResult>.Failure(
                EmailConfirmationErrors.UserNotFound);
        }

        user.ConfirmEmail(
            _clock.UtcNow);

        token.MarkAsUsed(
            _clock.UtcNow);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<ConfirmEmailResult>.Success(
            new ConfirmEmailResult(
                user.Id,
                user.Email.Value,
                user.EmailConfirmed));
    }
}
