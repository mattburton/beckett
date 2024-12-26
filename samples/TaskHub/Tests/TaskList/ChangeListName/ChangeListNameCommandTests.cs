using TaskHub.TaskList.ChangeListName;
using TaskHub.TaskList.Events;

namespace Tests.TaskList.ChangeListName;

public class ChangeListNameCommandTests : CommandSpecificationFixture<ChangeListNameCommand>
{
    [Fact]
    public void task_list_name_changed()
    {
        var taskListId = Guid.NewGuid();
        var name = Guid.NewGuid().ToString();

        Specification
            .When(new ChangeListNameCommand(taskListId, name))
            .Then(new TaskListNameChanged(taskListId, name));
    }
}
