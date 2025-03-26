using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Slices.AddTaskList;

namespace Tests.TaskLists.Slices.AddTaskList;

public class AddTaskListCommandTests : CommandFixture<AddTaskListCommand>
{
    [Fact]
    public void task_list_added()
    {
        var id = Generate.Guid();
        var name = Generate.String();

        Specification
            .When(
                new AddTaskListCommand(id, name)
            )
            .Then(
                new TaskListAdded(id, name)
            );
    }
}
