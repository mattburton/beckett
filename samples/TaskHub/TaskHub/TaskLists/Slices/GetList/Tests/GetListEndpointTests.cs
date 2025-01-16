using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.GetList.Tests;

public class GetListEndpointTests
{
    public class when_query_returns_result
    {
        [Fact]
        public async Task returns_ok_with_result()
        {
            var id = Generate.Guid();
            var name = Generate.String();
            var task = Generate.String();
            var expectedQuery = new GetListQuery(id);
            var expectedResult = StateBuilder.Build<GetListReadModel>(
                new TaskListAdded(id, name),
                new TaskAdded(id, task)
            );
            var queryDispatcher = new FakeQueryDispatcher();
            queryDispatcher.Returns(expectedQuery, expectedResult);

            var result = await GetListEndpoint.Handle(id, queryDispatcher, CancellationToken.None);

            var actualResult = Assert.IsType<Ok<GetListReadModel>>(result);
            Assert.Equal(expectedResult, actualResult.Value);
        }
    }

    public class when_query_returns_null
    {
        [Fact]
        public async Task returns_not_found()
        {
            var id = Generate.Guid();
            var queryDispatcher = new FakeQueryDispatcher();

            var result = await GetListEndpoint.Handle(id, queryDispatcher, CancellationToken.None);

            Assert.IsType<NotFound>(result);
        }
    }
}
