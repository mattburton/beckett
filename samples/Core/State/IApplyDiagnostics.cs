namespace Core.State;

/// <summary>
/// Interface implemented by the source generator that exposes the list of applied message types for a class
/// </summary>
public interface IApplyMessageTypes
{
    Type[] MessageTypes();
}

