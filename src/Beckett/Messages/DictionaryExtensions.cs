namespace Beckett.Messages;

public static class DictionaryExtensions
{
    public static void Prepend(
        this Dictionary<string, object> dictionary,
        Dictionary<string, object> dictionaryToPrepend
    )
    {
        if (dictionaryToPrepend.Count == 0)
        {
            return;
        }

        var result = dictionaryToPrepend.ToDictionary(item => item.Key, item => item.Value);

        foreach (var item in dictionary)
        {
            result.TryAdd(item.Key, item.Value);
        }

        dictionary.Clear();

        foreach (var item in result)
        {
            dictionary.Add(item.Key, item.Value);
        }
    }
}
