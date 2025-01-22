using System.Reflection;
using System.Runtime.CompilerServices;
using Beckett.Configuration;
using TaskHub.Infrastructure.Modules;
using TaskHub.TaskLists;

namespace TaskHub.Infrastructure.Tests;

public static class GlobalTestSetup
{
    [ModuleInitializer]
    internal static void Initialize() => RegisterAllMessageTypes(typeof(TaskListModule).Assembly);

    private static void RegisterAllMessageTypes(params Assembly[] assemblies)
    {
        var builder = new BeckettBuilder(new ServiceCollection());

        foreach (var assembly in assemblies)
        {
            var types = GetTypes(assembly).Where(x => x.IsModule());

            foreach (var type in types)
            {
                var instance = Activator.CreateInstance(type);

                if (instance is not IModule module)
                {
                    continue;
                }

                module.MessageTypes(builder);
            }
        }
    }

    private static Type[] GetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException)
        {
            return [];
        }
    }
}
