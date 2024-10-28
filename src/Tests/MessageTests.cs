using System.Text.Json;
using Beckett;
using Beckett.Messages;

namespace Tests;

[Collection("MessageTypeMap")]
public sealed class MessageTests : IDisposable
{
    public MessageTests()
    {
        MessageTypeMap.Map<TestMessage>("test-message");
    }

    [Fact]
    public void MapsTypeAndSerializesDataInConstructor()
    {
        var expectedId = Guid.NewGuid();
        var input = new TestMessage(expectedId);
        var expectedData = JsonDocument.Parse(JsonSerializer.Serialize(input));

        var message = new Message(input);

        Assert.Equal("test-message", message.Type);
        Assert.Equivalent(expectedData, message.Data);
    }

    [Fact]
    public void UnwrapsEnvelopeInConstructorIfPassedIn()
    {
        const string expectedType = "message-type";
        var expectedData = JsonDocument.Parse("{}");
        var input = new Message(expectedType, expectedData);

        var message = new Message(input);

        Assert.Equal(expectedType, message.Type);
        Assert.Equal(expectedData, message.Data);
    }

    [Fact]
    public void UsesMetadataPassedInConstructor()
    {
        var input = new TestMessage(Guid.NewGuid());
        var expectedMetadata = new Dictionary<string, object>
        {
            { "test-key", "test-value" }
        };

        var message = new Message(input, expectedMetadata);

        Assert.Equal(expectedMetadata, message.Metadata);
    }

    [Fact]
    public void CanAddMetadataToMessage()
    {
        var input = new TestMessage(Guid.NewGuid());
        var message = new Message(input);

        message.AddMetadata("test-key", "test-value");

        Assert.Single(message.Metadata, new KeyValuePair<string, object>("test-key", "test-value"));
    }

    [Fact]
    public void CanAddCorrelationIdToMessageMetadata()
    {
        const string expectedCorrelationId = "correlation-id";
        var input = new TestMessage(Guid.NewGuid());
        var message = new Message(input);

        message.WithCorrelationId(expectedCorrelationId);

        Assert.Single(message.Metadata, new KeyValuePair<string, object>("$correlation_id", expectedCorrelationId));
    }

    // ReSharper disable once NotAccessedPositionalProperty.Local
    private record TestMessage(Guid Id);

    public void Dispose()
    {
        MessageTypeMap.Clear();
    }
}
