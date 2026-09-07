namespace PGLN.Auth.Application.Abstractions.Authentication;

public interface IRefreshTokenGenerator
{
    string Generate();
}
