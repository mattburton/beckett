using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Beckett.Messages;

namespace Beckett.Tests.Messages;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
public sealed class MessageUpcasterTests : IDisposable
{
    [Fact]
    public void throws_when_upcaster_is_mapped_multiple_times()
    {
        MessageUpcaster.Register("old-type", "new-type", x => x);

        Assert.Throws<UpcasterAlreadyRegisteredException>(
            () => MessageUpcaster.Register("old-type", "new-type", x => x)
        );
    }

    [Fact]
    public void returns_original_data_if_no_upcaster_is_registered_for_type_name()
    {
        var expected = JsonDocument.Parse(@"{""Name"":""Value""}").RootElement;

        var result = MessageUpcaster.Upcast("message-type", expected);

        Assert.Equal(expected, result.Data);
    }

    [Fact]
    public void returns_new_type_name()
    {
        MessageUpcaster.Register("old-type", "new-type", x => x);

        var result = MessageUpcaster.Upcast("old-type", EmptyJsonElement.Instance);

        Assert.Equal("new-type", result.TypeName);
    }

    [Fact]
    public void upcasts_message()
    {
        var expectedValue = Guid.NewGuid().ToString();
        const string expectedTypeName = "type-v2";

        MessageUpcaster.Register(
            "type-v1",
            expectedTypeName,
            x =>
            {
                if (!x.TryGetPropertyValue("PropertyV1", out var propertyV1) || propertyV1 == null)
                {
                    return x;
                }

                x.Remove("PropertyV1");

                x.Add("PropertyV2", propertyV1.GetValue<string>());

                return x;
            }
        );

        var dataV1 = JsonDocument.Parse($@"{{""PropertyV1"":""{expectedValue}""}}").RootElement;

        var result = MessageUpcaster.Upcast("type-v1", dataV1);

        var dataV2 = result.Data.Deserialize<TestMessageV2>();

        Assert.Equal(expectedTypeName, result.TypeName);
        Assert.NotNull(dataV2);
        Assert.Equal(expectedValue, dataV2.PropertyV2);
    }

    [Fact]
    public void upcasts_message_recursively()
    {
        var expectedValue = Guid.NewGuid().ToString();
        const string expectedTypeName = "type-v4";

        MessageUpcaster.Register(
            "type-v1",
            "type-v2",
            x =>
            {
                if (!x.TryGetPropertyValue("PropertyV1", out var propertyV1) || propertyV1 == null)
                {
                    return x;
                }

                x.Remove("PropertyV1");

                x.Add("PropertyV2", propertyV1.GetValue<string>());

                return x;
            }
        );

        MessageUpcaster.Register(
            "type-v2",
            "type-v3",
            x =>
            {
                if (!x.TryGetPropertyValue("PropertyV2", out var propertyV2) || propertyV2 == null)
                {
                    return x;
                }

                x.Remove("PropertyV2");

                x.Add("PropertyV3", propertyV2.GetValue<string>());

                return x;
            }
        );

        MessageUpcaster.Register(
            "type-v3",
            expectedTypeName,
            x =>
            {
                if (!x.TryGetPropertyValue("PropertyV3", out var propertyV3) || propertyV3 == null)
                {
                    return x;
                }

                x.Remove("PropertyV3");

                x.Add("PropertyV4", propertyV3.GetValue<string>());

                return x;
            }
        );

        var dataV1 = JsonDocument.Parse($@"{{""PropertyV1"":""{expectedValue}""}}").RootElement;

        var result = MessageUpcaster.Upcast("type-v1", dataV1);

        var dataV4 = result.Data.Deserialize<TestMessageV4>();

        Assert.Equal(expectedTypeName, result.TypeName);
        Assert.NotNull(dataV4);
        Assert.Equal(expectedValue, dataV4.PropertyV4);
    }

    private record TestMessageV2(string PropertyV2);

    private record TestMessageV4(string PropertyV4);

    public void Dispose()
    {
        MessageUpcaster.Clear();
    }
}
