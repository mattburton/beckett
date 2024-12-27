using Beckett.Messages;
using Beckett.MessageStorage;
using Beckett.MessageStorage.InMemory;

namespace Beckett.Tests.MessageStorage.InMemory;

[Collection("MessageTypeMap")]
public class InMemoryMessageStorageTests : IDisposable
{
    public InMemoryMessageStorageTests()
    {
        MessageTypeMap.Map<TestEvent>("test_event");
    }

    [Fact]
    public async Task appends_to_and_reads_stream()
    {
        var streamName = $"test-{Guid.NewGuid()}";
        var storage = new InMemoryMessageStorage();
        await storage.AppendToStream(
            streamName,
            ExpectedVersion.Any,
            [new Message(new TestEvent(1))],
            CancellationToken.None
        );

        var stream = await storage.ReadStream(streamName, new ReadStreamOptions(), CancellationToken.None);

        Assert.Single(stream.Messages);
        var message = Assert.IsType<TestEvent>(stream.Messages[0].Message);
        Assert.Equal(1, message.Number);
    }

    [Fact]
    public async Task reads_global_stream()
    {
        var streamName = $"test-{Guid.NewGuid()}";
        var storage = new InMemoryMessageStorage();
        await storage.AppendToStream(
            streamName,
            ExpectedVersion.Any,
            [new Message(new TestEvent(1))],
            CancellationToken.None
        );

        var globalStream = await storage.ReadGlobalStream(0, 10, CancellationToken.None);

        var item = Assert.Single(globalStream.Items);
        Assert.Equal(1, item.GlobalPosition);
        Assert.Equal(1, item.StreamPosition);
        Assert.Equal(streamName, item.StreamName);
        Assert.Equal("test_event", item.MessageType);
    }

    public record TestEvent(int Number);

    public void Dispose()
    {
        MessageTypeMap.Clear();
    }
}