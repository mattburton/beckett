using TaskHub.Users.Contracts.Queries;
using TaskHub.Users.Events;

namespace TaskHub.Users.Slices.User.Tests;

public class GetUserEndpointTests
{
    public class when_query_returns_result
    {
        [Fact]
        public async Task returns_ok_with_result()
        {
            var username = Generate.String();
            var email = Generate.String();
            var expectedQuery = new UserQuery(username);
            var expectedResult = StateBuilder.Build<UserReadModel>(
                new UserRegistered(username, email)
            );
            var queryDispatcher = new FakeQueryDispatcher();
            queryDispatcher.Returns(expectedQuery, expectedResult);

            var result = await GetUserEndpoint.Handle(username, queryDispatcher, CancellationToken.None);

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
            var queryDispatcher = new FakeQueryDispatcher();

            var result = await GetUserEndpoint.Handle(username, queryDispatcher, CancellationToken.None);

            Assert.IsType<NotFound>(result);
        }
    }
}
