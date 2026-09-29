namespace PGLN.Auth.Sample.AwsOutboxPublisher;

public static class Program
{
    private static readonly Guid E2eMessageId =
        Guid.Parse(
            "2c934369-debc-4044-a9c4-b9e9aad7c7b7");

    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length == 1 &&
                args[0] == "--replay-e2e")
            {
                await LocalRunner.RunAsync(
                    E2eMessageId);

                Console.WriteLine(
                    "E2E duplicate replay published.");

                return 0;
            }

            if (args.Length != 0)
            {
                Console.Error.WriteLine(
                    "Usage: publisher [--replay-e2e]");

                return 2;
            }

            var processed =
                await LocalRunner.RunAsync();

            Console.WriteLine(
                $"Local publisher processed {processed} outbox message(s).");

            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                $"Local publisher failed: {exception.Message}");

            return 1;
        }
    }
}
