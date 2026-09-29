using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Email;

namespace PGLN.Auth.Infrastructure.Email;

public static class DependencyInjection
{
    public static IServiceCollection AddPGLNAuthDefaultEmailTemplates(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<
            IEmailTemplateRenderer,
            DefaultEmailTemplateRenderer>();

        return services;
    }
}
