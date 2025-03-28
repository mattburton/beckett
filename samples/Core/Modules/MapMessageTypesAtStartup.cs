using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Beckett.Messages;
using Core.Contracts;
using Core.Extensions;
using Microsoft.Extensions.DependencyModel;

namespace Core.Modules;

[SuppressMessage("Usage", "CA2255:The \'ModuleInitializer\' attribute should not be used in libraries")]
public static class MapMessageTypesAtStartup
{
    private static readonly Type TypeToMap = typeof(ISupportSubscriptions);
    private static readonly Type SkipMappingType = typeof(IShouldNotBeMappedAutomatically);

    [ModuleInitializer]
    public static void Initialize()
    {
        var assemblies = GetAssembliesToScan();

        foreach (var assembly in assemblies)
        {
            var messageTypes = assembly.GetLoadableTypes().Where(x => x.IsTypeToMap());

            foreach (var messageType in messageTypes)
            {
                var instance = RuntimeHelpers.GetUninitializedObject(messageType);

                if (instance is not IHaveTypeName message)
                {
                    continue;
                }

                MessageTypeMap.Map(messageType, message.TypeName());
            }
        }
    }

    private static HashSet<Assembly> GetAssembliesToScan()
    {
        var context = DependencyContext.Default!;
        var assemblyNames = context.RuntimeLibraries.SelectMany(x => x.GetDefaultAssemblyNames(context)).ToHashSet();
        var assemblies = new HashSet<Assembly>();

        foreach (var assemblyName in assemblyNames)
        {
            if (assemblyName.TryLoadAssembly(out var assembly))
            {
                assemblies.Add(assembly);
            }
        }

        return assemblies;
    }

    private static bool IsTypeToMap(this Type x) =>
        TypeToMap.IsAssignableFrom(x) &&
        !SkipMappingType.IsAssignableFrom(x) &&
        x is { IsAbstract: false, IsInterface: false };
}

public interface IShouldNotBeMappedAutomatically;
