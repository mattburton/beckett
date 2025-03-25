using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Slices.DeleteTaskList;

namespace Tests.TaskLists.Slices.DeleteTaskList;

public class DeleteTaskListCommandTests : CommandSpecificationFixture<DeleteTaskListCommand>
{
    [Fact]
    public void task_list_deleted()
    {
        var id = Generate.Guid();

        Specification
            .When(
                new DeleteTaskListCommand(id)
            )
            .Then(
                new TaskListDeleted(id)
            );
    }
}
