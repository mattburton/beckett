using Contracts.TaskLists.Queries;
using Core.Streams;
using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Queries.GetTaskList;
using TaskHub.TaskLists.Streams;

namespace Tests.TaskLists.Queries.GetTaskList;

public class GetTaskListQueryHandlerTests
{
    public class when_list_exists
    {
        [Fact]
        public async Task returns_result()
        {
            var id = Generate.Guid();
            var name = Generate.String();
            var task = Generate.String();
            var reader = new FakeStreamReader();
            var handler = new GetTaskListQueryHandler(reader);
            var query = new GetTaskListQuery(id);
            var expectedResult = BuildExpectedResult(id, name, task);
            reader.HasExistingStream(
                new TaskListStream(id),
                new TaskListAdded(id, name),
                new TaskAdded(id, task)
            );

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.Equivalent(expectedResult, result);
        }

        private static GetTaskListQuery.Result BuildExpectedResult(Guid id, string name, string task)
        {
            return new GetTaskListQuery.Result(id, name, [new GetTaskListQuery.TaskItem(task, false)]);
        }
    }

    public class when_list_does_not_exist
    {
        [Fact]
        public async Task returns_null()
        {
            var reader = new FakeStreamReader();
            var handler = new GetTaskListQueryHandler(reader);
            var query = new GetTaskListQuery(Generate.Guid());

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.Null(result);
        }
    }
}
