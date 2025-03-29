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
            var query = new GetUser(Example.String);
            var expectedResult = new GetUser.Result(Example.String, Example.String);
            var dispatcher = new FakeDispatcher();
            var module = new UserModule(dispatcher);
            dispatcher.Returns(query, expectedResult);

            var result = await GetUserEndpoint.Handle(Example.String, module, CancellationToken.None);

            var actualResult = Assert.IsType<Ok<GetUser.Result>>(result);
            Assert.Equal(expectedResult, actualResult.Value);
        }
    }

    public class when_query_returns_null
    {
        [Fact]
        public async Task returns_not_found()
        {
            var dispatcher = new FakeDispatcher();
            var module = new UserModule(dispatcher);

            var result = await GetUserEndpoint.Handle(Example.String, module, CancellationToken.None);

            Assert.IsType<NotFound>(result);
        }
    }
}
