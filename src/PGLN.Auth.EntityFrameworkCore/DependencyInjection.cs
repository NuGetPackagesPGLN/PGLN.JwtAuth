using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.EntityFrameworkCore.Persistence;
using PGLN.Auth.EntityFrameworkCore.Persistence.Repositories;

namespace PGLN.Auth.EntityFrameworkCore;

public static class DependencyInjection
{
    public static IServiceCollection AddPGLNAuthEntityFrameworkCore(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddDbContext<AuthDbContext>(
            configureOptions);

        services.AddScoped<
            IUserRepository,
            UserRepository>();

        services.AddScoped<
            IEmailVerificationTokenRepository,
            EmailVerificationTokenRepository>();

        services.AddScoped<
            IUnitOfWork,
            UnitOfWork>();

        return services;
    }
}
