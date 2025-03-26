using API.V1.Users;
using TaskHub.Users.Events;
using TaskHub.Users.Slices.Users;

namespace Tests.API.V1.Users;

public class UsersEndpointTests
{
    public class when_query_returns_result
    {
        [Fact]
        public async Task returns_ok_with_result()
        {
            var queryBus = new FakeQueryBus();
            var query = new UsersQuery();
            var expectedResults = new List<UsersReadModel>
            {
                ReadModelBuilder.Build<UsersReadModel>(new UserRegistered("username", "email"))
            };
            queryBus.Returns(query, expectedResults);

            var result = await GetUsersEndpoint.Handle(queryBus, CancellationToken.None);

            var actualResults = Assert.IsType<Ok<IReadOnlyList<UsersReadModel>>>(result);
            Assert.Equal(expectedResults, actualResults.Value);
        }
    }
}
