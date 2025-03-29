namespace Core.State;

/// <summary>
/// Internal interface used by the source generator to provide startup diagnostics for projections
/// </summary>
public interface IApplyDiagnostics
{
    Type[] AppliedMessageTypes();
}
