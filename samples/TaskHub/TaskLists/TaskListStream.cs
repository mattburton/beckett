namespace TaskLists;

public record TaskListStream(Guid Id) : IStreamName
{
    public string StreamName() => $"TaskList-{Id}";
}
