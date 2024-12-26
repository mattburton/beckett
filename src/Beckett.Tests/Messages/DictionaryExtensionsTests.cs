using Beckett.Messages;

namespace Beckett.Tests.Messages;

public class DictionaryExtensionsTests
{
    [Fact]
    public void prepends_dictionary()
    {
        var dictionary = new Dictionary<string, string>
        {
            { "key-2", "value-2" },
            { "key-3", "value-3" }
        };
        var dictionaryToPrepend = new Dictionary<string, string>
        {
            { "key-1", "value-1" }
        };

        dictionary.Prepend(dictionaryToPrepend);

        Assert.Equal("value-1", dictionary["key-1"]);
        Assert.Equal("value-2", dictionary["key-2"]);
        Assert.Equal("value-3", dictionary["key-3"]);
    }

    [Fact]
    public void prepends_dictionary_with_precedence()
    {
        var dictionary = new Dictionary<string, string>
        {
            { "key-1", "overwrite-me" },
            { "key-2", "value-2" }
        };
        var dictionaryToPrepend = new Dictionary<string, string>
        {
            { "key-1", "value-1" }
        };

        dictionary.Prepend(dictionaryToPrepend);

        Assert.Equal("value-1", dictionary["key-1"]);
        Assert.Equal("value-2", dictionary["key-2"]);
    }

    [Fact]
    public void handles_empty_dictionary_to_prepend()
    {
        var dictionary = new Dictionary<string, string>
        {
            { "key-1", "value-1" },
            { "key-2", "value-2" }
        };
        var dictionaryToPrepend = new Dictionary<string, string>();

        dictionary.Prepend(dictionaryToPrepend);

        Assert.Equal("value-1", dictionary["key-1"]);
        Assert.Equal("value-2", dictionary["key-2"]);
    }

    [Fact]
    public void handles_empty_base_dictionary()
    {
        var dictionary = new Dictionary<string, string>();
        var dictionaryToPrepend = new Dictionary<string, string>
        {
            { "key-1", "value-1" },
            { "key-2", "value-2" }
        };

        dictionary.Prepend(dictionaryToPrepend);

        Assert.Equal("value-1", dictionary["key-1"]);
        Assert.Equal("value-2", dictionary["key-2"]);
    }
}
