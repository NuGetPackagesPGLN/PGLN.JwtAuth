using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.TokenRefresh;

public sealed record TokenRefreshCommand(
    string RefreshToken)
    : ICommand<Result<TokenRefreshResult>>;
