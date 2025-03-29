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
            var dispatcher = new FakeDispatcher();
            var module = new UserModule(dispatcher);
            var query = new GetUsers();
            var expectedResult = new GetUsers.Result(
                [
                    new GetUsers.User(Example.String, Example.String)
                ]
            );
            dispatcher.Returns(query, expectedResult);

            var result = await GetUsersEndpoint.Handle(module, CancellationToken.None);

            var actualResults = Assert.IsType<Ok<GetUsers.Result>>(result);
            Assert.Equal(expectedResult, actualResults.Value);
        }
    }
}
