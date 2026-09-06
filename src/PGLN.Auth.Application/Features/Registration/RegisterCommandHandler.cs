using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Features.Registration;

public sealed class RegisterCommandHandler
    : ICommandHandler<RegisterCommand, Result<RegisterResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<RegisterResult>> HandleAsync(
        RegisterCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var email = Email.Create(command.Email);

        var emailAlreadyExists =
            await _userRepository.ExistsByNormalizedEmailAsync(
                email.NormalizedValue,
                cancellationToken);

        if (emailAlreadyExists)
        {
            return Result<RegisterResult>.Failure(
                RegistrationErrors.EmailAlreadyExists);
        }

        var passwordHash =
            _passwordHasher.Hash(command.Password);

        var user = User.Register(
            UserId.New(),
            email,
            passwordHash,
            _clock.UtcNow);

        await _userRepository.AddAsync(
            user,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        var result = new RegisterResult(
            user.Id,
            user.Email.Value,
            user.EmailConfirmed);

        return Result<RegisterResult>.Success(result);
    }
}