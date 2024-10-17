namespace Beckett.Dashboard.MessageStore;

public record GetCategoriesResult(List<GetCategoriesResult.Category> Categories, int TotalResults)
{
    public record Category(string Name, DateTimeOffset LastUpdated);
}
