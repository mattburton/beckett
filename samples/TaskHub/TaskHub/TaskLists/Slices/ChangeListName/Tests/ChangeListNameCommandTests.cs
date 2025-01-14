using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.ChangeListName.Tests;

public class ChangeListNameCommandTests : CommandSpecificationFixture<ChangeListNameCommand>
{
    [Fact]
    public void task_list_name_changed()
    {
        var taskListId = Generate.Guid();
        var name = Generate.String();

        Specification
            .When(
                new ChangeListNameCommand(taskListId, name)
            )
            .Then(
                new TaskListNameChanged(taskListId, name)
            );
    }
}
