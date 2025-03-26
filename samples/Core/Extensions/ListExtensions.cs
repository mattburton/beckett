namespace Core.Extensions;

public static class ListExtensions
{
    public static List<T> Replace<T>(this List<T> list, T current, T updated)
    {
        var index = list.IndexOf(current);

        if (index == -1)
        {
            throw new ArgumentOutOfRangeException(nameof(current), "List item not found");
        }

        list[index] = updated;

        return list;
    }
}
