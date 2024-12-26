using System.Text.Json;
using Beckett.Messages;

namespace Beckett.Tests;

[Collection("MessageTypeMap")]
public sealed class MessageTests : IDisposable
{
    public MessageTests()
    {
        MessageTypeMap.Map<TestMessage>("test-message");
    }

    [Fact]
    public void maps_type_and_serializes_data_in_constructor()
    {
        var expectedId = Guid.NewGuid();
        var input = new TestMessage(expectedId);
        var expectedData = JsonDocument.Parse(JsonSerializer.Serialize(input));

        var message = new Message(input);

        Assert.Equal("test-message", message.Type);
        Assert.Equivalent(expectedData, message.Data);
    }

    [Fact]
    public void uses_metadata_passed_in_constructor()
    {
        var input = new TestMessage(Guid.NewGuid());
        var expectedMetadata = new Dictionary<string, string>
        {
            { "test-key", "test-value" }
        };

        var message = new Message(input, expectedMetadata);

        Assert.Equal(expectedMetadata, message.Metadata);
    }

    [Fact]
    public void can_add_metadata_to_message()
    {
        var input = new TestMessage(Guid.NewGuid());
        var message = new Message(input);

        message.AddMetadata("test-key", "test-value");

        Assert.Single(message.Metadata, new KeyValuePair<string, string>("test-key", "test-value"));
    }

    [Fact]
    public void can_add_correlation_id_to_message_metadata()
    {
        const string expectedCorrelationId = "correlation-id";
        var input = new TestMessage(Guid.NewGuid());
        var message = new Message(input);

        message.WithCorrelationId(expectedCorrelationId);

        Assert.Single(message.Metadata, new KeyValuePair<string, string>("$correlation_id", expectedCorrelationId));
    }

    private record TestMessage(Guid Id);

    public void Dispose()
    {
        MessageTypeMap.Clear();
    }
}
