using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Beckett.Subscriptions;

public class SubscriptionHandler
{
    private static readonly Type MessageContextType = typeof(IMessageContext);
    private static readonly Type TypedMessageContextType = typeof(IMessageContext<>);
    private static readonly Type ConcreteTypedMessageContextType = typeof(MessageContext<>);
    private static readonly Type BatchType = typeof(IReadOnlyList<IMessageContext>);
    private static readonly Type UnwrappedBatchType = typeof(IReadOnlyList<object>);
    private static readonly Type CancellationTokenType = typeof(CancellationToken);
    private static readonly Type ResultHandlerType = typeof(IResultHandler<>);

    private readonly Subscription _subscription;
    private readonly ParameterInfo[] _parameters;
    private readonly Func<IServiceProvider, object>?[] _parameterResolvers;
    private readonly Func<object[], Task<object>> _invoker;

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

    public async Task Invoke(
        IMessageContext context,
        IServiceProvider serviceProvider,
        ILogger logger,
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
            else if (_parameters[i].ParameterType.IsGenericType &&
                     _parameters[i].ParameterType.GetGenericTypeDefinition() == TypedMessageContextType)
            {
                var messageType = _parameters[i].ParameterType.GetGenericArguments()[0];
                var contextType = ConcreteTypedMessageContextType.MakeGenericType(messageType);

                arguments[i] = Activator.CreateInstance(contextType, context)!;
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

        var result = await _invoker(arguments);

        if (result is EmptyResult)
        {
            return;
        }

        var resultType = result.GetType();

        if (serviceProvider.GetService(
                ResultHandlerType.MakeGenericType(resultType)
            ) is not IResultHandler handler)
        {
            logger.ResultHandlerNotRegistered(resultType, _subscription.Name);

            return;
        }

        await handler.Handle(result, cancellationToken);
    }

    public async Task Invoke(
        IReadOnlyList<IMessageContext> batch,
        IServiceProvider serviceProvider,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        var arguments = new object[_parameters.Length];

        for (var i = 0; i < _parameters.Length; i++)
        {
            if (_parameters[i].ParameterType == BatchType)
            {
                arguments[i] = batch;
            }
            else if (_parameters[i].ParameterType == UnwrappedBatchType)
            {
                arguments[i] = batch.Where(x => x.Message != null).Select(x => x.Message!).ToList();
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

        var result = await _invoker(arguments);

        if (result is EmptyResult)
        {
            return;
        }

        var resultType = result.GetType();

        if (serviceProvider.GetService(
                ResultHandlerType.MakeGenericType(resultType)
            ) is not IResultHandler handler)
        {
            logger.ResultHandlerNotRegistered(resultType, _subscription.Name);

            return;
        }

        await handler.Handle(result, cancellationToken);
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
        var batchHandler = false;
        var typedMessageHandler = false;

        foreach (var parameter in _parameters)
        {
            if (parameter.ParameterType == MessageContextType)
            {
                messageContextHandler = true;
            }

            if (parameter.ParameterType == BatchType || parameter.ParameterType == UnwrappedBatchType)
            {
                IsBatchHandler = true;

                batchHandler = true;
            }

            if (parameter.ParameterType.IsGenericType &&
                parameter.ParameterType.GetGenericTypeDefinition() == TypedMessageContextType)
            {
                typedMessageHandler = true;
            }

            if (_subscription.MessageTypes.Contains(parameter.ParameterType))
            {
                typedMessageHandler = true;
            }
        }

        if (messageContextHandler && batchHandler)
        {
            throw new InvalidOperationException(
                $"Subscription handlers can only accept a message or a message batch, not both. [Subscription: {_subscription.Name}]"
            );
        }

        if (batchHandler && typedMessageHandler)
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

        if (!messageContextHandler && !batchHandler && !typedMessageHandler)
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
        return _parameters.Select(parameter => IsWellKnownType(parameter)
            ? null
            : new Func<IServiceProvider, object>(sp => sp.GetRequiredService(parameter.ParameterType))
        );
    }

    private static bool IsWellKnownType(ParameterInfo x) => x.ParameterType == MessageContextType ||
                                                            x.ParameterType.IsGenericType &&
                                                            x.ParameterType.GetGenericTypeDefinition() ==
                                                            TypedMessageContextType ||
                                                            x.ParameterType == BatchType ||
                                                            x.ParameterType == CancellationTokenType;

    private static Func<object[], Task<object>> BuildInvoker(Delegate handler)
    {
        var method = handler.Method;
        var returnType = method.ReturnType;
        var argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");
        var arguments = method.GetParameters().Select((parameter, index) => Expression.Convert(
                Expression.ArrayIndex(argumentsParameter, Expression.Constant(index)),
                parameter.ParameterType
            )
        ).ToArray<Expression>();
        var instance = method.IsStatic ? null : Expression.Constant(handler.Target);
        var call = Expression.Call(instance, method, arguments);

        if (returnType == typeof(Task))
        {
            var taskHandlerDelegate = Expression.Lambda<Func<object[], Task>>(call, argumentsParameter).Compile();

            return async parameters =>
            {
                await taskHandlerDelegate(parameters);

                return EmptyResult.Instance;
            };
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var taskReturnType = returnType.GetGenericArguments()[0];
            var taskHandlerDelegate = Expression.Lambda<Func<object[], Task<object>>>(
                Expression.Call(
                    ExecuteTaskWithResultMethod.MakeGenericMethod(taskReturnType),
                    call
                ),
                argumentsParameter
            ).Compile();

            return async parameters =>
            {
                var task = taskHandlerDelegate(parameters);

                if (task == null)
                {
                    throw new InvalidOperationException(
                        "The Task returned by the subscription handler must not be null."
                    );
                }

                var result = await task;

                if (result is null)
                {
                    throw new InvalidOperationException(
                        "The result returned by the subscription handler must not be null."
                    );
                }

                return result;
            };
        }

        if (returnType != typeof(void))
        {
            throw new InvalidOperationException("Subscription handlers must have a return type of Task or void.");
        }

        var voidHandlerDelegate = Expression.Lambda<Action<object[]>>(
            Expression.Block(call, Expression.Default(typeof(void))),
            argumentsParameter
        ).Compile();

        return parameters =>
        {
            voidHandlerDelegate(parameters);

            return Task.FromResult(EmptyResult.Instance);
        };
    }

    private static async Task<object> ExecuteTaskWithResult<T>(Task<T> task)
    {
        if (task is null)
        {
            throw new InvalidOperationException("The Task returned by the subscription handler must not be null.");
        }

        var result = await task;

        if (result is null)
        {
            throw new InvalidOperationException("The result returned by the subscription handler must not be null.");
        }

        return result;
    }

    private static readonly MethodInfo ExecuteTaskWithResultMethod = typeof(SubscriptionHandler).GetMethod(
        nameof(ExecuteTaskWithResult),
        BindingFlags.NonPublic | BindingFlags.Static
    )!;
}

public interface IResultHandler<in T> : IResultHandler
{
    Task Handle(T result, CancellationToken cancellationToken);

    Task IResultHandler.Handle(object result, CancellationToken cancellationToken)
    {
        if (result is T typedResult)
        {
            return Handle(typedResult, cancellationToken);
        }

        throw new InvalidOperationException(
            $"The result type '{result.GetType()}' does not match the expected type '{typeof(T)}'."
        );
    }
}

public class EmptyResult
{
    public static readonly object Instance = new EmptyResult();
}

public static partial class Log
{
    [LoggerMessage(
        0,
        LogLevel.Trace,
        "Result handler for {ResultType} returned by the handler for subscription {SubscriptionName} was not registered in the container - skipping."
    )]
    public static partial void ResultHandlerNotRegistered(
        this ILogger logger,
        Type ResultType,
        string SubscriptionName
    );
}
