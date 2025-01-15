using TaskHub.Users.Events;

namespace TaskHub.Users.Slices.GetUsers.Tests;

public class GetUsersIntegrationTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task creates_read_model()
    {
        var state = GenerateReadModel();
        var projection = new GetUsersReadModelProjection(database.DataSource);
        await projection.Create(state, CancellationToken.None);

        var readModel = await projection.Read(state.Username, CancellationToken.None);

        Assert.NotNull(readModel);
        Assert.Equal(state.Username, readModel.Username);
        Assert.Equal(state.Email, readModel.Email);
    }

    [Fact]
    public async Task deletes_read_model()
    {
        var state = GenerateReadModel();
        var projection = new GetUsersReadModelProjection(database.DataSource);
        await projection.Create(state, CancellationToken.None);

        await projection.Delete(state.Username, CancellationToken.None);

        var readModel = await projection.Read(state.Username, CancellationToken.None);
        Assert.Null(readModel);
    }

    [Fact]
    public async Task query_returns_all_rows()
    {
        var expectedResults = new List<GetUsersReadModel>
        {
            GenerateReadModel(),
            GenerateReadModel(),
            GenerateReadModel()
        };
        var handler = new GetUsersQuery.Handler(database.DataSource);
        var query = new GetUsersQuery();
        await SetupDatabase(expectedResults);

        var results = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(results);
        foreach (var expectedResult in expectedResults)
        {
            Assert.Contains(results, result => result.Username == expectedResult.Username);
        }
    }

    private async Task SetupDatabase(List<GetUsersReadModel> expectedResults)
    {
        var projection = new GetUsersReadModelProjection(database.DataSource);

        foreach (var readModel in expectedResults)
        {
            await projection.Create(readModel, CancellationToken.None);
        }
    }

    private static GetUsersReadModel GenerateReadModel() => StateBuilder.Build<GetUsersReadModel>(
        new UserRegistered(Guid.NewGuid().ToString(), Guid.NewGuid().ToString())
    );
}
