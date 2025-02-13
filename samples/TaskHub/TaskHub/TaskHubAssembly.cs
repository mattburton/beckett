using System.Reflection;

namespace TaskHub;

public static class TaskHubAssembly
{
    public static readonly Assembly Instance = typeof(TaskHubAssembly).Assembly;
}
