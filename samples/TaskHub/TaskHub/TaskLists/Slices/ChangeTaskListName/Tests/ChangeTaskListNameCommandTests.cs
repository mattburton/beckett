using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.ChangeTaskListName.Tests;

public class ChangeTaskListNameCommandTests : CommandSpecificationFixture<ChangeTaskListNameCommand>
{
    [Fact]
    public void task_list_name_changed()
    {
        var taskListId = Generate.Guid();
        var name = Generate.String();

        Specification
            .When(
                new ChangeTaskListNameCommand(taskListId, name)
            )
            .Then(
                new TaskListNameChanged(taskListId, name)
            );
    }
}
