using TaskHub.Users.Events;
using TaskHub.Users.Queries;

namespace TaskHub.Users.Slices.User.Tests;

public class UserEndpointTests
{
    public class when_query_returns_result
    {
        [Fact]
        public async Task returns_ok_with_result()
        {
            var username = Generate.String();
            var email = Generate.String();
            var query = new UserQuery(username);
            var expectedResult = StateBuilder.Build<UserReadModel>(
                new UserRegistered(username, email)
            );
            var queryBus = new FakeQueryBus();
            queryBus.Returns(query, expectedResult);

            var result = await UserEndpoint.Handle(username, queryBus, CancellationToken.None);

            var actualResult = Assert.IsType<Ok<UserReadModel>>(result);
            Assert.Equal(expectedResult, actualResult.Value);
        }
    }

    public class when_query_returns_null
    {
        [Fact]
        public async Task returns_not_found()
        {
            var username = Generate.String();
            var queryBus = new FakeQueryBus();

            var result = await UserEndpoint.Handle(username, queryBus, CancellationToken.None);

            Assert.IsType<NotFound>(result);
        }
    }
}
