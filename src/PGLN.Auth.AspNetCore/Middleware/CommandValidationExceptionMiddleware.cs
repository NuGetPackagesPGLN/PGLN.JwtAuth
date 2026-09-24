using Microsoft.AspNetCore.Http;
using PGLN.Auth.Application.Common.Validation;
using PGLN.Auth.Contracts.Common;

namespace PGLN.Auth.AspNetCore.Middleware;

internal sealed class CommandValidationExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public CommandValidationExceptionMiddleware(
        RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(
            next);

        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(
            context);

        try
        {
            await _next(
                context);
        }
        catch (CommandValidationException exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            var errors =
                exception.Errors
                    .GroupBy(
                        error =>
                            error.PropertyName)
                    .ToDictionary(
                        group =>
                            group.Key,
                        group =>
                            group
                                .Select(
                                    error =>
                                        error.ErrorMessage)
                                .ToArray());

            var response =
                new ValidationErrorResponse(
                    "Validation.Failed",
                    "One or more validation errors occurred.",
                    errors);

            context.Response.StatusCode =
                StatusCodes.Status400BadRequest;

            await context.Response.WriteAsJsonAsync(
                response,
                context.RequestAborted);
        }
    }
}
