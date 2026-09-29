using PGLN.Auth.Sample.AwsEmailWorker;

if (args.Length == 1 &&
    string.Equals(
        args[0],
        "--sqs-e2e",
        StringComparison.OrdinalIgnoreCase))
{
    await LocalRunner.RunSqsMessageAsync();
}
else if (args.Length == 0)
{
    LocalRunner.ValidateServices();
}
else
{
    Console.Error.WriteLine(
        "Usage: dotnet run -- [--sqs-e2e]");

    Environment.ExitCode = 1;
}
