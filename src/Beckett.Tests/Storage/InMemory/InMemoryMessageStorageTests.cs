using Beckett.Storage;
using Beckett.Storage.InMemory;

namespace Beckett.Tests.Storage.InMemory;

public class InMemoryMessageStorageTests
{
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

        Assert.Single(stream.StreamMessages);
        var message = Assert.IsType<TestEvent>(MessageContext.From(stream.StreamMessages[0]).Message);
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

        var globalStream = await storage.ReadGlobalStream(new ReadGlobalStreamOptions
        {
            LastGlobalPosition = 0,
            BatchSize = 10
        }, CancellationToken.None);

        var item = Assert.Single(globalStream.StreamMessages);
        Assert.Equal(1, item.GlobalPosition);
        Assert.Equal(1, item.StreamPosition);
        Assert.Equal(streamName, item.StreamName);
        Assert.Equal("test-event", item.MessageType);
    }
}
