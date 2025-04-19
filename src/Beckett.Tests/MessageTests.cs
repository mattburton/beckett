using System.Text.Json;

namespace Beckett.Tests;

public class MessageTests
{
    [Fact]
    public void maps_type_and_serializes_data_in_constructor()
    {
        var expectedId = Guid.NewGuid();
        var input = new TestMessage(expectedId);
        var expectedData = JsonDocument.Parse(JsonSerializer.Serialize(input)).RootElement;

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

        message.WithMetadata("test-key", "test-value");

        Assert.Single(message.Metadata, new KeyValuePair<string, string>("test-key", "test-value"));
    }

    [Fact]
    public void can_set_message_id()
    {
        var expectedId = Guid.NewGuid();
        var input = new TestMessage(Guid.NewGuid());
        var message = new Message(input);

        message.WithMessageId(expectedId);

        Assert.Equal(expectedId, message.Id);
    }

    [Fact]
    public void can_add_causation_id_to_message_metadata()
    {
        const string expectedCausationId = "causation-id";
        var input = new TestMessage(Guid.NewGuid());
        var message = new Message(input);

        message.WithCausationId(expectedCausationId);

        Assert.Single(message.Metadata, new KeyValuePair<string, string>("$causation_id", expectedCausationId));
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

    [Fact]
    public void can_add_tenant_to_message_metadata()
    {
        const string expectedTenant = "tenant";
        var input = new TestMessage(Guid.NewGuid());
        var message = new Message(input);

        message.WithTenant(expectedTenant);

        Assert.Single(message.Metadata, new KeyValuePair<string, string>("$tenant", expectedTenant));
    }
}
