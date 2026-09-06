using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application;
using PGLN.Auth.Application.Abstractions.Email;
using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.AspNetCore.Endpoints;
using PGLN.Auth.EntityFrameworkCore;
using PGLN.Auth.EntityFrameworkCore.Persistence;
using PGLN.Auth.Infrastructure;

namespace PGLN.Auth.IntegrationTests.Http;

internal sealed class HttpTestApplication
    : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private HttpTestApplication(
        WebApplication application,
        SqliteConnection connection)
    {
        Application =
            application;

        _connection =
            connection;
    }

    public WebApplication Application { get; }

    public HttpClient CreateClient()
    {
        return Application.GetTestClient();
    }

    public static async Task<HttpTestApplication> CreateAsync()
    {
        var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        var builder =
            WebApplication.CreateBuilder();

        builder.WebHost.UseTestServer();

        builder.Services
            .AddPGLNAuthApplication();

        builder.Services
            .AddPGLNAuthInfrastructure();

        builder.Services
            .AddPGLNAuthEntityFrameworkCore(
                options =>
                    options.UseSqlite(
                        connection));

        builder.Services.AddSingleton<
            IIntegrationEventPayloadProtector,
            HttpTestPayloadProtector>();

        builder.Services.AddSingleton<
            IEmailSender,
            HttpTestEmailSender>();

        builder.Services.AddSingleton<
            IEmailTemplateRenderer,
            HttpTestEmailTemplateRenderer>();

        var application =
            builder.Build();

        application.MapPGLNAuthEndpoints();

        await application.StartAsync();

        await using (
            var scope =
                application.Services.CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            await dbContext.Database.EnsureCreatedAsync();
        }

        return new HttpTestApplication(
            application,
            connection);
    }

    public async ValueTask DisposeAsync()
    {
        await Application.StopAsync();
        await Application.DisposeAsync();
        await _connection.DisposeAsync();
    }
}

