using API.V1.Users;
using Contracts.Users.Queries;
using TaskHub.Users;

namespace Tests.API.V1.Users;

public class GetUsersEndpointTests
{
    public class when_query_returns_result
    {
        [Fact]
        public async Task returns_ok_with_result()
        {
            var commandDispatcher = new FakeCommandDispatcher();
            var queryDispatcher = new FakeQueryDispatcher();
            var module = new UserModule(commandDispatcher, queryDispatcher);
            var query = new GetUsersQuery();
            var expectedResult = new GetUsersQuery.Result(
                [
                    new GetUsersQuery.User(Generate.String(), Generate.String())
                ]
            );
            queryDispatcher.Returns(query, expectedResult);

            var result = await GetUsersEndpoint.Handle(module, CancellationToken.None);

            var actualResults = Assert.IsType<Ok<GetUsersQuery.Result>>(result);
            Assert.Equal(expectedResult, actualResults.Value);
        }
    }
}
