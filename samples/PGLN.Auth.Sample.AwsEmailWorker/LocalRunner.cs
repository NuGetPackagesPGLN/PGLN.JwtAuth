using Amazon;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Abstractions.Inbox;
using PGLN.Auth.Application.Events.Dispatching;

namespace PGLN.Auth.Sample.AwsEmailWorker;

internal static class LocalRunner
{
    public static void ValidateServices()
    {
        var provider =
            WorkerServices.Provider;

        using var scope =
            provider.CreateScope();

        _ =
            scope.ServiceProvider
                .GetRequiredService<IntegrationEventTypeRegistry>();

        var dispatcher =
            scope.ServiceProvider
                .GetRequiredService<IIntegrationEventDispatcher>();

        if (dispatcher is not IntegrationEventDispatcher)
        {
            throw new InvalidOperationException(
                $"Expected {nameof(IntegrationEventDispatcher)} " +
                $"but resolved {dispatcher.GetType().FullName}.");
        }

        var inbox =
            scope.ServiceProvider
                .GetRequiredService<IIntegrationEventInbox>();

        if (!string.Equals(
                inbox.GetType().Name,
                "EfIntegrationEventInbox",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Expected EF-backed integration-event inbox " +
                $"but resolved {inbox.GetType().FullName}.");
        }

        Console.WriteLine(
            "AWS Email Worker dependency graph validated successfully.");

        Console.WriteLine(
            $"Dispatcher: {dispatcher.GetType().Name}");

        Console.WriteLine(
            $"Inbox: {inbox.GetType().Name}");
    }

    public static async Task RunSqsMessageAsync()
    {
        ValidateServices();

        var configuration =
            WorkerServices.Configuration;

        var region =
            configuration["PGLNAuth:Aws:Sqs:Region"];

        var queueUrl =
            configuration["PGLNAuth:Aws:Sqs:QueueUrl"];

        if (string.IsNullOrWhiteSpace(region))
        {
            throw new InvalidOperationException(
                "PGLNAuth:Aws:Sqs:Region is required.");
        }

        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            throw new InvalidOperationException(
                "PGLNAuth:Aws:Sqs:QueueUrl is required.");
        }

        var regionEndpoint =
            RegionEndpoint.GetBySystemName(
                region);

        using var sqs =
            new AmazonSQSClient(
                regionEndpoint);

        Console.WriteLine(
            "Receiving one SQS message...");

        var receiveResponse =
            await sqs.ReceiveMessageAsync(
                new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = 1,
                    WaitTimeSeconds = 5,
                    VisibilityTimeout = 60,

                    MessageSystemAttributeNames =
                    [
                        MessageSystemAttributeName.All
                    ],

                    MessageAttributeNames =
                    [
                        "All"
                    ]
                });

        var message =
            receiveResponse.Messages?
                .SingleOrDefault();

        if (message is null)
        {
            Console.WriteLine(
                "No SQS message is currently available.");

            return;
        }

        Console.WriteLine(
            $"Received SQS message {message.MessageId}.");

        // Intentionally never print message.Body.
        // Authentication events may contain sensitive data.
        var sqsEvent =
            new SQSEvent
            {
                Records =
                [
                    new SQSEvent.SQSMessage
                    {
                        MessageId = message.MessageId,
                        ReceiptHandle = message.ReceiptHandle,
                        Body = message.Body,
                        Attributes =
                            message.Attributes,
                        MessageAttributes =
                            ConvertMessageAttributes(
                                message.MessageAttributes),
                        EventSource = "aws:sqs",
                        EventSourceArn = null,
                        AwsRegion = region
                    }
                ]
            };

        var function =
            new Function();

        var lambdaContext =
            new LocalLambdaContext();

        var response =
            await function.FunctionHandler(
                sqsEvent,
                lambdaContext);

        var failed =
            response.BatchItemFailures.Any(
                failure =>
                    string.Equals(
                        failure.ItemIdentifier,
                        message.MessageId,
                        StringComparison.Ordinal));

        if (failed)
        {
            Console.WriteLine(
                $"Worker reported failure for SQS message {message.MessageId}.");

            Console.WriteLine(
                "Message was NOT deleted and can be retried.");

            return;
        }

        await sqs.DeleteMessageAsync(
            new DeleteMessageRequest
            {
                QueueUrl = queueUrl,
                ReceiptHandle =
                    message.ReceiptHandle
            });

        Console.WriteLine(
            $"Worker successfully processed SQS message {message.MessageId}.");

        Console.WriteLine(
            "SQS message deleted after successful processing.");
    }

    private static Dictionary<
        string,
        SQSEvent.MessageAttribute>
        ConvertMessageAttributes(
            Dictionary<
                string,
                MessageAttributeValue> attributes)
    {
        if (attributes is null)
        {
            return new Dictionary<
                string,
                SQSEvent.MessageAttribute>();
        }

        return attributes.ToDictionary(
            pair => pair.Key,
            pair =>
                new SQSEvent.MessageAttribute
                {
                    StringValue =
                        pair.Value.StringValue,
                    BinaryValue =
                        pair.Value.BinaryValue,
                    StringListValues =
                        pair.Value.StringListValues,
                    BinaryListValues =
                        pair.Value.BinaryListValues,
                    DataType =
                        pair.Value.DataType
                });
    }

    private sealed class LocalLambdaContext
        : ILambdaContext
    {
        public string AwsRequestId { get; } =
            $"local-{Guid.NewGuid():N}";

        public IClientContext ClientContext =>
            null!;

        public string FunctionName =>
            "PGLN.Auth.Sample.AwsEmailWorker.Local";

        public string FunctionVersion =>
            "local";

        public ICognitoIdentity Identity =>
            null!;

        public string InvokedFunctionArn =>
            "local";

        public ILambdaLogger Logger { get; } =
            new LocalLambdaLogger();

        public string LogGroupName =>
            "local";

        public string LogStreamName =>
            "local";

        public int MemoryLimitInMB =>
            512;

        public TimeSpan RemainingTime =>
            TimeSpan.FromMinutes(5);
    }

    private sealed class LocalLambdaLogger
        : ILambdaLogger
    {
        public void Log(string message)
        {
            Console.Write(message);
        }

        public void LogLine(string message)
        {
            Console.WriteLine(message);
        }
    }
}
