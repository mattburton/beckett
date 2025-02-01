using System.Diagnostics.CodeAnalysis;
using Beckett.Messages;

namespace Beckett.Tests.Messages;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
public class MessageTypeMapTests
{
    [Fact]
    public void maps_type_name()
    {
        MessageTypeMap.Map<TestMessage>("type-map-test");

        Assert.True(MessageTypeMap.TryGetType("type-map-test", out var type));

        Assert.NotNull(type);
        Assert.Equal(typeof(TestMessage), type);
    }

    [Fact]
    public void throws_when_type_is_mapped_to_multiple_names()
    {
        MessageTypeMap.Map<TestMessage>("type-map-test");

        Assert.Throws<MessageTypeAlreadyMappedException>(
            () => MessageTypeMap.Map<TestMessage>("same-type-different-name")
        );
    }

    [Fact]
    public void throws_when_name_is_reused_for_different_types()
    {
        MessageTypeMap.Map<TestMessage>("type-map-test");

        Assert.Throws<MessageTypeAlreadyMappedException>(
            () => MessageTypeMap.Map<TestMessage2>("type-map-test")
        );
    }

    private record TestMessage;

    private record TestMessage2;
}
