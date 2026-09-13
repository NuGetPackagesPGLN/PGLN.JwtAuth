using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Features.StepUp.VerifyStepUpChallenge;
using PGLN.Auth.AspNetCore.RateLimiting;
using PGLN.Auth.Contracts.Authentication;

namespace PGLN.Auth.AspNetCore.Endpoints.Authentication;

public static class VerifyStepUpEndpoint
{
    public static RouteHandlerBuilder MapVerifyStepUpEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost(
                "/verify-step-up",
                HandleAsync)
            .RequireRateLimiting(
                StepUpRateLimitOptions.PolicyName);
    }

    private static async Task<IResult> HandleAsync(
        VerifyStepUpRequest request,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result =
            await dispatcher.SendAsync(
                new VerifyStepUpChallengeCommand(
                    request.ChallengeId,
                    request.Code,
                    request.RememberDevice),
                cancellationToken);

        if (result.IsSuccess)
        {
            var value =
                result.Value;

            return Results.Ok(
                new VerifyStepUpResponse(
                    value.UserId,
                    value.Email,
                    value.AccessToken,
                    value.AccessTokenExpiresAtUtc,
                    value.RefreshToken,
                    value.RefreshTokenExpiresAtUtc));
        }

        return result.Error.Code switch
        {
            "StepUpChallenge.NotFound" =>
                Results.Json(
                    new
                    {
                        code =
                            result.Error.Code,

                        description =
                            result.Error.Description
                    },
                    statusCode:
                        StatusCodes.Status404NotFound),

            "StepUpChallenge.InvalidCode" =>
                Results.Json(
                    new
                    {
                        code =
                            result.Error.Code,

                        description =
                            result.Error.Description
                    },
                    statusCode:
                        StatusCodes.Status401Unauthorized),

            "StepUpChallenge.Expired" =>
                Results.Json(
                    new
                    {
                        code =
                            result.Error.Code,

                        description =
                            result.Error.Description
                    },
                    statusCode:
                        StatusCodes.Status401Unauthorized),

            "StepUpChallenge.MaximumAttemptsExceeded" =>
                Results.Json(
                    new
                    {
                        code =
                            result.Error.Code,

                        description =
                            result.Error.Description
                    },
                    statusCode:
                        StatusCodes.Status429TooManyRequests),

            "StepUpChallenge.AlreadyVerified" =>
                Results.Json(
                    new
                    {
                        code =
                            result.Error.Code,

                        description =
                            result.Error.Description
                    },
                    statusCode:
                        StatusCodes.Status409Conflict),

            "User.NotFound" =>
                Results.Json(
                    new
                    {
                        code =
                            result.Error.Code,

                        description =
                            result.Error.Description
                    },
                    statusCode:
                        StatusCodes.Status404NotFound),

            _ =>
                Results.Json(
                    new
                    {
                        code =
                            result.Error.Code,

                        description =
                            result.Error.Description
                    },
                    statusCode:
                        StatusCodes.Status400BadRequest)
        };
    }
}
