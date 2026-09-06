using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Infrastructure.Authentication;
using PGLN.Auth.Infrastructure.Passwords;
using PGLN.Auth.Infrastructure.Time;

namespace PGLN.Auth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPGLNAuthInfrastructure(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IClock, SystemClock>();

        services.AddSingleton<
            IPasswordHasher,
            PasswordHasher>();

        services.AddSingleton<
            IVerificationTokenGenerator,
            SecureVerificationTokenGenerator>();

        services.AddSingleton<
            ITokenHasher,
            Sha256TokenHasher>();

        return services;
    }
}
