using System.Runtime.CompilerServices;
using Beckett.Messages;
using TaskHub.TaskLists;

namespace TaskHub.Infrastructure.Tests;

public static class GlobalTestSetup
{
    [ModuleInitializer]
    internal static void Initialize() => MessageTypeMapInitializer.Initialize(typeof(TaskListModule).Assembly);
}
