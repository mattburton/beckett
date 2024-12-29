using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Subscriptions;

public class SubscriptionHandler
{
    private static readonly Type MessageContextType = typeof(IMessageContext);
    private static readonly Type MessageBatchType = typeof(IMessageBatch);
    private static readonly Type CancellationTokenType = typeof(CancellationToken);

    private readonly Subscription _subscription;
    private readonly ParameterInfo[] _parameters;
    private readonly Func<IServiceProvider, object>?[] _parameterResolvers;
    private readonly Func<object[], Task> _invoker;

    public SubscriptionHandler(Subscription subscription, Delegate handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _subscription = subscription;
        _parameters = handler.Method.GetParameters();
        ValidateHandler();
        _parameterResolvers = BuildParameterResolvers().ToArray();
        _invoker = BuildInvoker(handler);
        TrySetHandlerName(handler);
    }

    public bool IsBatchHandler { get; private set; }

    public Task Invoke(
        IMessageContext context,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken
    )
    {
        var arguments = new object[_parameters.Length];

        for (var i = 0; i < _parameters.Length; i++)
        {
            if (_parameters[i].ParameterType == MessageContextType)
            {
                arguments[i] = context;
            }
            else if (_parameters[i].ParameterType == context.MessageType)
            {
                arguments[i] = context.Message!;
            }
            else if (_parameters[i].ParameterType == CancellationTokenType)
            {
                arguments[i] = cancellationToken;
            }
            else if (_parameterResolvers[i] != null)
            {
                arguments[i] = _parameterResolvers[i]!(serviceProvider);
            }
        }

        return _invoker(arguments);
    }

    public Task Invoke(IMessageBatch batch, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var arguments = new object[_parameters.Length];

        for (var i = 0; i < _parameters.Length; i++)
        {
            if (_parameters[i].ParameterType == MessageBatchType)
            {
                arguments[i] = batch;
            }
            else if (_parameters[i].ParameterType == CancellationTokenType)
            {
                arguments[i] = cancellationToken;
            }
            else if (_parameterResolvers[i] != null)
            {
                arguments[i] = _parameterResolvers[i]!(serviceProvider);
            }
        }

        return _invoker(arguments);
    }

    private void TrySetHandlerName(Delegate handler)
    {
        if (!string.IsNullOrWhiteSpace(_subscription.HandlerName))
        {
            return;
        }

        if (handler.Method is { IsStatic: true, DeclaringType: not null })
        {
            _subscription.HandlerName = $"{handler.Method.DeclaringType.FullName}::{handler.Method.Name}";
        }
    }

    private void ValidateHandler()
    {
        var messageContextHandler = false;
        var messageBatchHandler = false;
        var typedMessageHandler = false;

        foreach (var parameter in _parameters)
        {
            if (parameter.ParameterType == MessageContextType)
            {
                messageContextHandler = true;
            }

            if (parameter.ParameterType == MessageBatchType)
            {
                IsBatchHandler = true;

                messageBatchHandler = true;
            }

            if (_subscription.MessageTypes.Contains(parameter.ParameterType))
            {
                typedMessageHandler = true;
            }
        }

        if (messageContextHandler && messageBatchHandler)
        {
            throw new InvalidOperationException(
                $"Subscription handlers can only accept a message or a message batch, not both. [Subscription: {_subscription.Name}]"
            );
        }

        if (messageBatchHandler && typedMessageHandler)
        {
            throw new InvalidOperationException(
                $"Message batch handlers cannot also handle individual messages [Subscription: {_subscription.Name}]"
            );
        }

        if (typedMessageHandler && _subscription.MessageTypes.Count > 1)
        {
            throw new InvalidOperationException(
                $"$Typed handlers can only subscribe to one message type [Subscription: {_subscription.Name}]"
            );
        }

        if (!messageContextHandler && !messageBatchHandler && !typedMessageHandler)
        {
            throw new InvalidOperationException(
                $"Subscription handlers must handle either a message or a message batc [Subscription: {_subscription.Name}]"
            );
        }

        if (_subscription.Category == null && _subscription.MessageTypes.Count == 0)
        {
            throw new InvalidOperationException(
                $"Subscription handlers must subscribe to at least one message type if a category is not configured [Subscription: {_subscription.Name}]"
            );
        }
    }

    private IEnumerable<Func<IServiceProvider, object>?> BuildParameterResolvers()
    {
        return _parameters.Select(
            parameter => IsWellKnownType(parameter)
                ? null
                : new Func<IServiceProvider, object>(sp => sp.GetRequiredService(parameter.ParameterType))
        );
    }

    private static bool IsWellKnownType(ParameterInfo x) => x.ParameterType == typeof(IMessageContext) ||
                                                            x.ParameterType == typeof(IMessageBatch) ||
                                                            x.ParameterType == typeof(CancellationToken);

    private static Func<object[], Task> BuildInvoker(Delegate handler)
    {
        var method = handler.Method;
        var returnType = method.ReturnType;
        var argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");
        var arguments = method.GetParameters().Select(
            (parameter, index) => Expression.Convert(
                Expression.ArrayIndex(argumentsParameter, Expression.Constant(index)),
                parameter.ParameterType
            )
        ).ToArray<Expression>();
        var instance = method.IsStatic ? null : Expression.Constant(handler.Target);
        var call = Expression.Call(instance, method, arguments);

        if (returnType != typeof(Task))
        {
            throw new InvalidOperationException("Subscription handlers must return a Task.");
        }

        var handlerExpression = Expression.Lambda<Func<object[], Task>>(call, argumentsParameter);

        return handlerExpression.Compile();
    }
}
