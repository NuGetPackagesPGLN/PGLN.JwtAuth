using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Email;

namespace PGLN.Auth;

public static class PGLNAuthEmailServiceCollectionExtensions
{
    public static IServiceCollection AddPGLNAuthEmail<TEmailSender>(
        this IServiceCollection services)
        where TEmailSender : class, IEmailSender
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IEmailSender, TEmailSender>();

        return services;
    }
}
