using Contracts.Users.Queries;
using TaskHub.Users.Events;
using TaskHub.Users.Queries.GetUsers;

namespace Tests.Users.Queries.GetUsers;

public class UsersIntegrationTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task creates_read_model()
    {
        var state = GenerateReadModel();
        var projection = new UsersProjection(database.DataSource);
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
        var projection = new UsersProjection(database.DataSource);
        await projection.Create(state, CancellationToken.None);

        await projection.Delete(state.Username, CancellationToken.None);

        var readModel = await projection.Read(state.Username, CancellationToken.None);
        Assert.Null(readModel);
    }

    [Fact]
    public async Task query_returns_all_rows()
    {
        var data = new List<UsersReadModel>
        {
            GenerateReadModel(),
            GenerateReadModel(),
            GenerateReadModel()
        };
        var expectedResult = new GetUsersQuery.Result(
            data.Select(x => new GetUsersQuery.User(x.Username, x.Email)).ToList()
        );
        var handler = new GetUsersQueryHandler(database.DataSource);
        var query = new GetUsersQuery();
        await SetupDatabase(data);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        foreach (var expectedUser in expectedResult.Users)
        {
            Assert.Contains(result.Users, actualUser => actualUser.Username == expectedUser.Username);
        }
    }

    private async Task SetupDatabase(List<UsersReadModel> data)
    {
        var projection = new UsersProjection(database.DataSource);

        foreach (var row in data)
        {
            await projection.Create(row, CancellationToken.None);
        }
    }

    private static UsersReadModel GenerateReadModel() => ReadModelBuilder.Build<UsersReadModel>(
        new UserRegistered(Generate.String(), Generate.String())
    );
}
