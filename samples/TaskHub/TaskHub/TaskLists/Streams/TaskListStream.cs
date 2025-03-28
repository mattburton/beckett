using Core.Streams;

namespace TaskHub.TaskLists.Streams;

public record TaskListStream(Guid Id) : IStreamName
{
    public string StreamName() => $"TaskList-{Id}";
}
