using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Beckett.SourceGenerators;

[Generator]
public sealed class StateGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(
            ctx => ctx.AddSource(
                "StateAttribute.g.cs",
                SourceText.From(StateTemplates.Attribute, Encoding.UTF8)
            )
        );

        var readModels = context.SyntaxProvider.ForAttributeWithMetadataName(
                "Beckett.StateAttribute",
                predicate: static (node, _) => IsSyntaxTargetForGeneration(node),
                transform: static (context, _) => GetTargetForGeneration(context)
            )
            .Where(static m => m is not null);

        context.RegisterSourceOutput(readModels, (sourceContext, data) => Execute(data, sourceContext));
    }

    private static void Execute(State? readModel, SourceProductionContext context)
    {
        if (readModel is not { } value)
        {
            return;
        }

        var result = StateTemplates.StateClass(value);
        var fileName = StateTemplates.FileName(value);

        context.AddSource(fileName, SourceText.From(result, Encoding.UTF8));
    }

    private static State? GetTargetForGeneration(GeneratorAttributeSyntaxContext context)
    {
        return context.TargetSymbol is not INamedTypeSymbol symbol ? null : new State(symbol);
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node) =>
        node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };
}

public readonly record struct State
{
    public State(INamedTypeSymbol symbol)
    {
        ContainingNamespace = symbol.ContainingNamespace.ToString();
        ContainingTypeName = symbol.ContainingType?.Name;
        ContainingTypeAccessModifier = AccessibilityModifier(symbol.ContainingType?.DeclaredAccessibility);
        ContainingTypeIsRecord = symbol.ContainingType?.IsRecord ?? false;
        Name = symbol.Name;
        AccessModifier = AccessibilityModifier(symbol.DeclaredAccessibility);
        MessageTypes = symbol.GetMembers("Apply")
            .OfType<IMethodSymbol>()
            .Where(x => x.ReturnsVoid)
            .Where(x => x.Parameters.Length == 1)
            .Select(BuildMessageTypeFullName)
            .ToArray();
    }

    public readonly string ContainingNamespace;
    public readonly string? ContainingTypeName;
    public readonly string? ContainingTypeAccessModifier;
    public readonly bool ContainingTypeIsRecord;
    public readonly string Name;
    public readonly string? AccessModifier;
    public readonly string[] MessageTypes;

    private static string BuildMessageTypeFullName(IMethodSymbol method)
    {
        var message = method.Parameters[0];

        var builder = new StringBuilder();

        builder.Append(message.Type.ContainingNamespace);
        builder.Append('.');

        if (message.Type.ContainingType != null)
        {
            builder.Append(message.Type.ContainingType.Name);
            builder.Append('.');
        }

        builder.Append(message.Type.Name);

        return builder.ToString();
    }

    private static string? AccessibilityModifier(Accessibility? accessibility)
    {
        return accessibility switch
        {
            Accessibility.Private => "private",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.Public => "public",
            _ => null
        };
    }
}

public static class StateTemplates
{
    public const string Attribute = @"
namespace Beckett
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class StateAttribute : System.Attribute
    {
    }
}
    ";

    public static string FileName(State state)
    {
        return state.ContainingTypeName != null
            ? $"{state.ContainingNamespace}.{state.ContainingTypeName}.{state.Name}.g.cs"
            : $"{state.ContainingNamespace}.{state.Name}.g.cs";
    }

    public static string StateClass(State state)
    {
        return state.ContainingTypeName != null
            ? NestedStateClass(state)
            : $@"
using Beckett;

namespace {state.ContainingNamespace}
{{
    {state.AccessModifier} partial class {state.Name} : IApply
    {{
        public void Apply(object message)
        {{
            switch (message)
            {{
                {string.Join("\n", state.MessageTypes.Select(MessageTypeSwitchCase))}
            }}
        }}
    }}
}}
    ";
    }

    public static string NestedStateClass(State state) => $@"
using Beckett;

namespace {state.ContainingNamespace}
{{
    {state.ContainingTypeAccessModifier} partial {(state.ContainingTypeIsRecord ? "record" : "class")} {state.ContainingTypeName}
    {{
        {state.AccessModifier} partial class {state.Name} : IApply
        {{
            public void Apply(object message)
            {{
                switch (message)
                {{
                    {string.Join("\n", state.MessageTypes.Select(MessageTypeSwitchCase))}
                }}
            }}
        }}
    }}
}}
    ";

    private static string MessageTypeSwitchCase(string messageType) => $@"
                case {messageType} m:
                    Apply(m);
                    break;
";
}
