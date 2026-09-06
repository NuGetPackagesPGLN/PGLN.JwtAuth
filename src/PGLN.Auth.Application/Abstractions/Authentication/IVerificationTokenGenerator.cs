namespace PGLN.Auth.Application.Abstractions.Authentication;

public interface IVerificationTokenGenerator
{
    string Generate();
}
