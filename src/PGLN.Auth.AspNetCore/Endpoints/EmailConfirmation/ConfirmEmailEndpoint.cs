using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Application.Common.Validation;
using PGLN.Auth.Application.Features.EmailConfirmation;
using PGLN.Auth.Contracts.Common;
using PGLN.Auth.Contracts.EmailConfirmation;

namespace PGLN.Auth.AspNetCore.Endpoints.EmailConfirmation;

internal static class ConfirmEmailEndpoint
{
    internal static RouteHandlerBuilder Map(
        IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(
            endpoints);

        return endpoints.MapPost(
            "/confirm-email",
            HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        ConfirmEmailRequest request,
        ICommandHandler<
            ConfirmEmailCommand,
            Result<ConfirmEmailResult>> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        ArgumentNullException.ThrowIfNull(
            handler);

        try
        {
            var result =
                await handler.HandleAsync(
                    new ConfirmEmailCommand(
                        request.Token),
                    cancellationToken);

            if (result.IsFailure)
            {
                return MapFailure(
                    result.Error);
            }

            var response =
                new ConfirmEmailResponse(
                    result.Value.UserId.Value,
                    result.Value.Email,
                    result.Value.EmailConfirmed);

            return Results.Ok(
                response);
        }
        catch (CommandValidationException exception)
        {
            return Results.BadRequest(
                CreateValidationResponse(
                    exception));
        }
    }

    private static IResult MapFailure(
        Error error)
    {
        if (error ==
            EmailConfirmationErrors.TokenAlreadyUsed)
        {
            return Results.Conflict(
                new ApiErrorResponse(
                    error.Code,
                    error.Description));
        }

        if (error ==
            EmailConfirmationErrors.ExpiredToken)
        {
            return Results.Json(
                new ApiErrorResponse(
                    error.Code,
                    error.Description),
                statusCode:
                    StatusCodes.Status410Gone);
        }

        return Results.BadRequest(
            new ApiErrorResponse(
                error.Code,
                error.Description));
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
