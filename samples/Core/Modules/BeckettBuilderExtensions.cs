using System.Reflection;
using Beckett;

namespace Core.Modules;

public static class BeckettBuilderExtensions
{
    public static void WithMessageTypesFrom(
        this IBeckettBuilder builder,
        params Assembly[] assemblies
    )
    {
        var assembliesToScan = AssembliesToScan(assemblies);

        foreach (var assembly in assembliesToScan)
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

    public static void WithSubscriptionsFrom(
        this IBeckettBuilder builder,
        params Assembly[] assemblies
    )
    {
        var assembliesToScan = AssembliesToScan(assemblies);

        foreach (var assembly in assembliesToScan)
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

                module.Subscriptions(builder);
            }
        }
    }

    private static Assembly[] AssembliesToScan(Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
        {
            throw new ArgumentException(
                "At least one assembly must be provided to scan for modules",
                nameof(assemblies)
            );
        }

        return assemblies;
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
