using TaskLists.GetLists;

namespace TaskLists.Tests.GetLists;

public class GetListsEndpointTests
{
    public class when_query_returns_result
    {
        [Fact]
        public async Task returns_ok_with_result()
        {
            var module = new FakeTaskListModule();
            var query = new GetListsQuery();
            var expectedResult = new List<GetListsReadModel>(
                [
                    new GetListsReadModel
                    {
                        Id = Example.Guid,
                        Name = Example.String
                    }
                ]
            );
            var expectedResponse = GetListsEndpoint.Response.From(expectedResult);
            module.Returns(query, expectedResult);

            var result = await GetListsEndpoint.Handle(module, CancellationToken.None);

            var actualResponse = Assert.IsType<Ok<GetListsEndpoint.Response>>(result);
            Assert.NotNull(actualResponse.Value);
            Assert.Equivalent(expectedResponse, actualResponse.Value, true);
        }
    }
}
