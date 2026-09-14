namespace PGLN.Auth.Application.Abstractions.ExternalAuthentication;

public interface IPkceGenerator
{
    PkcePair Generate();
}

public sealed record PkcePair(
    string CodeVerifier,
    string CodeChallenge);
