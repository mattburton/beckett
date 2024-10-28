using System.Text.Json;
using Beckett.Messages;

namespace Tests.Messages;

public sealed class MessageTransformerTests : IDisposable
{
    [Fact]
    public void ThrowsWhenTransformationIsMappedMultipleTimes()
    {
        MessageTransformer.Register("old-type", "new-type", x => x);

        Assert.Throws<TransformationAlreadyRegisteredException>(
            () => MessageTransformer.Register("old-type", "new-type", x => x)
        );
    }

    [Fact]
    public void ReturnsOriginalDataIfNoTransformationIsRegisteredForTypeName()
    {
        var expected = JsonDocument.Parse(@"{""Name"":""Value""}");

        var actual = MessageTransformer.Transform("message-type", expected);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TransformsMessage()
    {
        var expectedValue = Guid.NewGuid().ToString();

        MessageTransformer.Register(
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

        var dataV1 = JsonDocument.Parse($@"{{""PropertyV1"":""{expectedValue}""}}");

        var dataV2 = MessageTransformer.Transform("type-v1", dataV1);

        var result = dataV2.Deserialize<TestMessageV2>();

        Assert.NotNull(result);
        Assert.Equal(expectedValue, result.PropertyV2);
    }

    [Fact]
    public void TransformsMessageRecursively()
    {
        var expectedValue = Guid.NewGuid().ToString();

        MessageTransformer.Register(
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

        MessageTransformer.Register(
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

        MessageTransformer.Register(
            "type-v3",
            "type-v4",
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

        var dataV1 = JsonDocument.Parse($@"{{""PropertyV1"":""{expectedValue}""}}");

        var dataV4 = MessageTransformer.Transform("type-v1", dataV1);

        var result = dataV4.Deserialize<TestMessageV4>();

        Assert.NotNull(result);
        Assert.Equal(expectedValue, result.PropertyV4);
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private record TestMessageV2(string PropertyV2);

    // ReSharper disable once ClassNeverInstantiated.Local
    private record TestMessageV4(string PropertyV4);

    public void Dispose()
    {
        MessageTransformer.Clear();
    }
}
