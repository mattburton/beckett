using API.V1.Users;
using Contracts.Users.Queries;
using TaskHub.Users;

namespace Tests.API.V1.Users;

public class GetUserEndpointTests
{
    public class when_query_returns_result
    {
        [Fact]
        public async Task returns_ok_with_result()
        {
            var username = Generate.String();
            var email = Generate.String();
            var query = new GetUserQuery(username);
            var expectedResult = new GetUserQuery.Result(username, email);
            var commandDispatcher = new FakeCommandDispatcher();
            var queryDispatcher = new FakeQueryDispatcher();
            var module = new UserModule(commandDispatcher, queryDispatcher);
            queryDispatcher.Returns(query, expectedResult);

            var result = await GetUserEndpoint.Handle(username, module, CancellationToken.None);

            var actualResult = Assert.IsType<Ok<GetUserQuery.Result>>(result);
            Assert.Equal(expectedResult, actualResult.Value);
        }
    }

    public class when_query_returns_null
    {
        [Fact]
        public async Task returns_not_found()
        {
            var username = Generate.String();
            var commandDispatcher = new FakeCommandDispatcher();
            var queryDispatcher = new FakeQueryDispatcher();
            var module = new UserModule(commandDispatcher, queryDispatcher);

            var result = await GetUserEndpoint.Handle(username, module, CancellationToken.None);

            Assert.IsType<NotFound>(result);
        }
    }
}
