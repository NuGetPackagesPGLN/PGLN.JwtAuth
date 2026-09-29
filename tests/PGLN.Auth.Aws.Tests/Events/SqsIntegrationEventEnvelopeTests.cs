using System.Text.Json;
using PGLN.Auth.Aws.Events;

namespace PGLN.Auth.Aws.Tests.Events;

public sealed class SqsIntegrationEventEnvelopeTests
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    [Fact]
    public void Serialize_And_Deserialize_Should_Preserve_Envelope()
    {
        // Arrange
        var messageId =
            Guid.Parse("6a7c6ad0-62fb-46c8-9d22-91fdf35e0ab4");

        var occurredAtUtc =
            new DateTimeOffset(
                2026,
                9,
                27,
                10,
                30,
                0,
                TimeSpan.Zero);

        const string eventType =
            "EmailConfirmationRequested";

        const string payload =
            """
            {
              "userId": "45a7cb1a-d035-4824-a3ea-f286805de51e",
              "email": "test@example.com"
            }
            """;

        var original =
            new SqsIntegrationEventEnvelope(
                messageId,
                eventType,
                occurredAtUtc,
                payload);

        // Act
        var json =
            JsonSerializer.Serialize(
                original,
                JsonOptions);

        var deserialized =
            JsonSerializer.Deserialize<
                SqsIntegrationEventEnvelope>(
                    json,
                    JsonOptions);

        // Assert
        Assert.NotNull(deserialized);

        Assert.Equal(
            original.MessageId,
            deserialized.MessageId);

        Assert.Equal(
            original.EventType,
            deserialized.EventType);

        Assert.Equal(
            original.OccurredAtUtc,
            deserialized.OccurredAtUtc);

        Assert.Equal(
            original.Payload,
            deserialized.Payload);
    }
}
