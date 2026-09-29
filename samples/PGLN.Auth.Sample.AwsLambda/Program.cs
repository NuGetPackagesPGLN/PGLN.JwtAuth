using Microsoft.AspNetCore.DataProtection;
using Amazon.Lambda.AspNetCoreServer.Hosting;
using PGLN.Auth;
using PGLN.Auth.AspNetCore.Endpoints;
using PGLN.Auth.AspNetCore.Extensions;
using PGLN.Auth.Aws.Email;

var builder = WebApplication.CreateBuilder(args);

// API Gateway HTTP API -> AWS Lambda -> ASP.NET Core
builder.Services.AddAWSLambdaHosting(
    LambdaEventSource.HttpApi);

var connectionString =
    builder.Configuration.GetConnectionString("PGLNAuth")
    ?? throw new InvalidOperationException(
        "Connection string 'PGLNAuth' is required.");

builder.Services
    .AddDataProtection()
    .SetApplicationName("PGLN.Auth.Aws");

builder.Services.AddPGLNAuthPostgreSql(
    builder.Configuration,
    connectionString);

builder.Services.AddPGLNAuthAwsSes(
    builder.Configuration);

builder.Services.AddOpenApi();

var app = builder.Build();

// For the sample we apply migrations automatically.
// A production AWS deployment may instead run migrations
// as a separate deployment step.
await app.Services.ApplyPGLNAuthMigrationsAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UsePGLNAuth();

app.UseAuthentication();
app.UseAuthorization();

app.MapPGLNAuthEndpoints();

app.MapGet(
    "/health",
    () => Results.Ok(
        new
        {
            status = "healthy",
            host = "aws-lambda"
        }));

app.MapGet(
        "/protected",
        () => Results.Ok(
            new
            {
                message = "PGLN.Auth authentication succeeded."
            }))
    .RequireAuthorization();

app.Run();
