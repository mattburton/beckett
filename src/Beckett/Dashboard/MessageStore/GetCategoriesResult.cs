namespace Beckett.Dashboard.MessageStore;

public record GetCategoriesResult(List<GetCategoriesResult.Category> Categories)
{
    public record Category(string Name, DateTimeOffset LastUpdated);
}
