namespace PGLN.Auth.Application.Abstractions.ExternalAuthentication;

public sealed class ExternalIdentityProviderException
    : Exception
{
    public ExternalIdentityProviderException(
        string message)
        : base(message)
    {
    }

    public ExternalIdentityProviderException(
        string message,
        Exception innerException)
        : base(
            message,
            innerException)
    {
    }
}
