using System.Reflection;
using Beckett;
using Core.Extensions;

namespace Core.Modules;

public static class BeckettBuilderExtensions
{
    private static readonly Type ModuleConfigurationType = typeof(IModuleConfiguration);

    public static void WithModulesFrom(
        this IBeckettBuilder builder,
        params Assembly[] assemblies
    )
    {
        var assembliesToScan = AssembliesToScan(assemblies);

        foreach (var assembly in assembliesToScan)
        {
            var moduleConfigurationTypes = assembly.GetLoadableTypes().Where(x => x.IsModuleConfigurationType());

            foreach (var moduleConfigurationType in moduleConfigurationTypes)
            {
                var instance = Activator.CreateInstance(moduleConfigurationType);

                if (instance is not IModuleConfiguration configuration)
                {
                    continue;
                }

                var moduleBuilder = new ModuleBuilder(builder);

                configuration.Configure(moduleBuilder);
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

    private static bool IsModuleConfigurationType(this Type x) =>
        ModuleConfigurationType.IsAssignableFrom(x) && x is { IsAbstract: false, IsInterface: false };
}
