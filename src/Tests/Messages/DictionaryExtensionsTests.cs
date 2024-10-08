using Beckett.Messages;

namespace Tests.Messages;

public class DictionaryExtensionsTests
{
    [Fact]
    public void PrependsDictionary()
    {
        var dictionary = new Dictionary<string, object>
        {
            { "key-2", "value-2" },
            { "key-3", "value-3" }
        };
        var dictionaryToPrepend = new Dictionary<string, object>
        {
            { "key-1", "value-1" }
        };

        dictionary.Prepend(dictionaryToPrepend);

        Assert.Equal("value-1", dictionary["key-1"]);
        Assert.Equal("value-2", dictionary["key-2"]);
        Assert.Equal("value-3", dictionary["key-3"]);
    }

    [Fact]
    public void PrependsDictionaryWithPrecedence()
    {
        var dictionary = new Dictionary<string, object>
        {
            { "key-1", "overwrite-me" },
            { "key-2", "value-2" }
        };
        var dictionaryToPrepend = new Dictionary<string, object>
        {
            { "key-1", "value-1" }
        };

        dictionary.Prepend(dictionaryToPrepend);

        Assert.Equal("value-1", dictionary["key-1"]);
        Assert.Equal("value-2", dictionary["key-2"]);
    }

    [Fact]
    public void HandlesEmptyDictionaryToPrepend()
    {
        var dictionary = new Dictionary<string, object>
        {
            { "key-1", "value-1" },
            { "key-2", "value-2" }
        };
        var dictionaryToPrepend = new Dictionary<string, object>();

        dictionary.Prepend(dictionaryToPrepend);

        Assert.Equal("value-1", dictionary["key-1"]);
        Assert.Equal("value-2", dictionary["key-2"]);
    }

    [Fact]
    public void HandlesEmptyBaseDictionary()
    {
        var dictionary = new Dictionary<string, object>();
        var dictionaryToPrepend = new Dictionary<string, object>
        {
            { "key-1", "value-1" },
            { "key-2", "value-2" }
        };

        dictionary.Prepend(dictionaryToPrepend);

        Assert.Equal("value-1", dictionary["key-1"]);
        Assert.Equal("value-2", dictionary["key-2"]);
    }
}
