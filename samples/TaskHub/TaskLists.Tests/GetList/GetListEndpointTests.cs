using TaskLists.GetList;

namespace TaskLists.Tests.GetList;

public class GetListEndpointTests
{
    public class when_query_returns_result
    {
        [Fact]
        public async Task returns_ok_with_result()
        {
            var query = new GetListQuery(Example.Guid);
            var expectedResult = new GetListReadModel
            {
                Id = Example.Guid,
                Name = Example.String,
                Tasks = [new GetListReadModel.TaskItem(Example.String, false)]
            };
            var expectedResponse = GetListEndpoint.Response.From(expectedResult);
            var module = new FakeTaskListModule();
            module.Returns(query, expectedResult);

            var result = await GetListEndpoint.Handle(Example.Guid, module, CancellationToken.None);

            var actualResponse = Assert.IsType<Ok<GetListEndpoint.Response>>(result);
            Assert.NotNull(actualResponse.Value);
            Assert.Equivalent(expectedResponse, actualResponse.Value, true);
        }
    }

    public class when_query_returns_null
    {
        [Fact]
        public async Task returns_not_found()
        {
            var module = new FakeTaskListModule();

            var result = await GetListEndpoint.Handle(Example.Guid, module, CancellationToken.None);

            Assert.IsType<NotFound>(result);
        }
    }
}
