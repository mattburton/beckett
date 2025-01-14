namespace Beckett;

/// <summary>
/// Used to apply messages from a stream to produce some representation of their state.
/// <code>
/// public class DecisionState : IApply
/// {
///     public bool WorkflowComplete { get; set; }
///
///     public void Apply(IMessageContext context)
///     {
///         switch (context.Message)
///             case WorkflowComplete:
///                 WorkflowComplete = true;
///                 break;
///             case WorkflowTimedOut:
///                 WorkflowComplete = false;
///                 break;
///         }
///     }
/// }
/// </code>
/// </summary>
public interface IApply
{
    void Apply(IMessageContext context);
}
