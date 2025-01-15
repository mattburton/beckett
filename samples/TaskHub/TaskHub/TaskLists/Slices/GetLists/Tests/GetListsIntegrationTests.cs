using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.GetLists.Tests;

public class GetListsIntegrationTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task reads_read_model()
    {
        var state = GenerateReadModel();
        var projection = new GetListsReadModelProjection(database.DataSource);
        await projection.Create(state, CancellationToken.None);

        var readModel = await projection.Read(state.Id, CancellationToken.None);

        Assert.NotNull(readModel);
        Assert.Equal(state.Id, readModel.Id);
        Assert.Equal(state.Name, readModel.Name);
    }

    [Fact]
    public async Task creates_read_model()
    {
        var state = GenerateReadModel();
        var projection = new GetListsReadModelProjection(database.DataSource);

        await projection.Create(state, CancellationToken.None);

        var readModel = await projection.Read(state.Id, CancellationToken.None);
        Assert.NotNull(readModel);
        Assert.Equal(state.Id, readModel.Id);
        Assert.Equal(state.Name, readModel.Name);
    }

    [Fact]
    public async Task updates_read_model()
    {
        var state = GenerateReadModel();
        var projection = new GetListsReadModelProjection(database.DataSource);
        await projection.Create(state, CancellationToken.None);

        state.Name = "updated name";
        await projection.Update(state, CancellationToken.None);

        var readModel = await projection.Read(state.Id, CancellationToken.None);
        Assert.NotNull(readModel);
        Assert.Equal(state.Id, readModel.Id);
        Assert.Equal(state.Name, readModel.Name);
    }

    [Fact]
    public async Task deletes_read_model()
    {
        var state = GenerateReadModel();
        var projection = new GetListsReadModelProjection(database.DataSource);
        await projection.Create(state, CancellationToken.None);

        await projection.Delete(state.Id, CancellationToken.None);

        var readModel = await projection.Read(state.Id, CancellationToken.None);
        Assert.Null(readModel);
    }

    [Fact]
    public async Task query_returns_all_rows()
    {
        var expectedResults = new List<GetListsReadModel>
        {
            GenerateReadModel(),
            GenerateReadModel(),
            GenerateReadModel()
        };
        var handler = new GetListsQuery.Handler(database.DataSource);
        var query = new GetListsQuery();
        await SetupDatabase(expectedResults);

        var results = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(results);
        foreach (var expectedResult in expectedResults)
        {
            Assert.Contains(results, result => result.Id == expectedResult.Id);
        }
    }

    private async Task SetupDatabase(List<GetListsReadModel> expectedResults)
    {
        var projection = new GetListsReadModelProjection(database.DataSource);

        foreach (var readModel in expectedResults)
        {
            await projection.Create(readModel, CancellationToken.None);
        }
    }

    private static GetListsReadModel GenerateReadModel() => StateBuilder.Build<GetListsReadModel>(
        new TaskListAdded(Generate.Guid(), Generate.String())
    );
}
