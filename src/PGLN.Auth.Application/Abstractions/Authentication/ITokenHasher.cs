namespace PGLN.Auth.Application.Abstractions.Authentication;

public interface ITokenHasher
{
    string Hash(string token);
}
