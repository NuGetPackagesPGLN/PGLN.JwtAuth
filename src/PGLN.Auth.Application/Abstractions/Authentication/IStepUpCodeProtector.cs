namespace PGLN.Auth.Application.Abstractions.Authentication;

public interface IStepUpCodeProtector
{
    string Protect(string code);

    bool Verify(
        string code,
        string protectedCode);
}
