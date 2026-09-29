namespace PGLN.Auth.Aws.Events;

public sealed class SqsOptions
{
    public const string SectionName =
        "PGLNAuth:Aws:Sqs";

    public string Region { get; init; } =
        string.Empty;

    public string QueueUrl { get; init; } =
        string.Empty;
}
