using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Slices.ChangeTaskListName;

namespace Tests.TaskLists.Slices.ChangeTaskListName;

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
