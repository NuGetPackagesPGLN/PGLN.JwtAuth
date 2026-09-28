namespace PGLN.Auth.Sample.AwsOutboxPublisher;

public static class Program
{
    public static async Task<int> Main()
    {
        try
        {
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
