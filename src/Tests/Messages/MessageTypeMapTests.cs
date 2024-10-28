using Beckett.Messages;

namespace Tests.Messages;

[Collection("MessageTypeMap")]
public sealed class MessageTypeMapTests : IDisposable
{
    [Fact]
    public void ThrowsWhenTypeNameIsMappedMultipleTimes()
    {
        MessageTypeMap.Map("old-type", "new-type");

        Assert.Throws<MessageTypeAlreadyMappedException>(
            () => MessageTypeMap.Map("old-type", "new-type")
        );
    }

    [Fact]
    public void MapsTypeName()
    {
        MessageTypeMap.Map<NewType>("new-type");
        MessageTypeMap.Map("old-type", "new-type");

        Assert.True(MessageTypeMap.TryGetType("old-type", out var type));

        Assert.NotNull(type);
        Assert.Equal(typeof(NewType), type);
    }

    [Fact]
    public void MapsTypeNameRecursively()
    {
        MessageTypeMap.Map<NewType>("new-type");
        MessageTypeMap.Map("type-v1", "type-v2");
        MessageTypeMap.Map("type-v2", "type-v3");
        MessageTypeMap.Map("type-v3", "new-type");

        Assert.True(MessageTypeMap.TryGetType("type-v1", out var type));

        Assert.NotNull(type);
        Assert.Equal(typeof(NewType), type);
    }

    public void Dispose()
    {
        MessageTypeMap.Clear();
    }

    private record NewType;
}
