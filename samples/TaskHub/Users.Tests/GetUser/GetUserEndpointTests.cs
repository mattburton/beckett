using Users.GetUser;

namespace Users.Tests.GetUser;

public class GetUserEndpointTests
{
    public class when_query_returns_result
    {
        [Fact]
        public async Task returns_ok_with_result()
        {
            var query = new GetUserQuery(Example.String);
            var expectedResult = new GetUserReadModel
            {
                Username = Example.String,
                Email = Example.String
            };
            var expectedResponse = GetUserEndpoint.Response.From(expectedResult);
            var module = new FakeUserModule();
            module.Returns(query, expectedResult);

            var result = await GetUserEndpoint.Handle(Example.String, module, CancellationToken.None);

            var actualResponse = Assert.IsType<Ok<GetUserEndpoint.Response>>(result);
            Assert.NotNull(actualResponse.Value);
            Assert.Equal(expectedResponse, actualResponse.Value);
        }
    }

    public class when_query_returns_null
    {
        [Fact]
        public async Task returns_not_found()
        {
            var module = new FakeUserModule();

            var result = await GetUserEndpoint.Handle(Example.String, module, CancellationToken.None);

            Assert.IsType<NotFound>(result);
        }
    }
}
