namespace PGLN.Auth.Aws.Email;

public sealed class SesOptions
{
    public const string SectionName = "PGLNAuth:Aws:Ses";

    public string Region { get; init; } = string.Empty;

    public string FromAddress { get; init; } = string.Empty;
}
