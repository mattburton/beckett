using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.DeleteTaskList.Tests;

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
