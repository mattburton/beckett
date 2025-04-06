using Users.GetUsers;

namespace Users.Tests.GetUsers;

public class GetUsersEndpointTests
{
    public class when_query_returns_result
    {
        [Fact]
        public async Task returns_ok_with_result()
        {
            var module = new FakeUserModule();
            var query = new GetUsersQuery();
            var expectedResult = new List<GetUsersReadModel>(
                [
                    new GetUsersReadModel
                    {
                        Username = Example.String,
                        Email = Example.String
                    }
                ]
            );
            var expectedResponse = GetUsersEndpoint.Response.From(expectedResult);
            module.Returns(query, expectedResult);

            var result = await GetUsersEndpoint.Handle(module, CancellationToken.None);

            var actualResponse = Assert.IsType<Ok<GetUsersEndpoint.Response>>(result);
            Assert.NotNull(actualResponse.Value);
            Assert.Equivalent(expectedResponse, actualResponse.Value, true);
        }
    }
}
