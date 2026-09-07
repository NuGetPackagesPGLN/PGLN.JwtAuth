using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Application.Common.Validation;
using PGLN.Auth.Application.Features.ResendEmailConfirmation;
using PGLN.Auth.Contracts.Common;
using PGLN.Auth.Contracts.EmailConfirmation;

namespace PGLN.Auth.AspNetCore.Endpoints.EmailConfirmation;

internal static class ResendEmailConfirmationEndpoint
{
    private const string PublicMessage =
        "If an unconfirmed account exists for that email address, a confirmation email has been sent.";

    internal static RouteHandlerBuilder Map(
        IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(
            endpoints);

        return endpoints.MapPost(
            "/resend-confirmation",
            HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        ResendEmailConfirmationRequest request,
        ICommandHandler<
            ResendEmailConfirmationCommand,
            Result> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        ArgumentNullException.ThrowIfNull(
            handler);

        try
        {
            await handler.HandleAsync(
                new ResendEmailConfirmationCommand(
                    request.Email),
                cancellationToken);

            return Results.Accepted(
                value:
                    new ResendEmailConfirmationResponse(
                        PublicMessage));
        }
        catch (CommandValidationException exception)
        {
            return Results.BadRequest(
                CreateValidationResponse(
                    exception));
        }
    }

    private static ValidationErrorResponse CreateValidationResponse(
        CommandValidationException exception)
    {
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

        return new ValidationErrorResponse(
            "Validation.Failed",
            "One or more validation errors occurred.",
            errors);
    }
}
