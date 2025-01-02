using TaskHub.Infrastructure.Tests;

namespace TaskHub.TaskLists.GetLists.Tests;

public class GetListsHandlerTests
{
    [Fact]
    public async Task executes_get_lists_query()
    {
        var database = new FakeDatabase();
        database.Returns(new List<GetListsReadModel>());

        await GetListsHandler.Get(database, CancellationToken.None);

        Assert.IsType<GetListsQuery>(database.ExecutedQuery);
    }
}
