namespace PGLN.Auth.Application.Abstractions.Security;

public interface IPasswordResetTokenGenerator
{
    string Generate();
}
