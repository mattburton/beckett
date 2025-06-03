using Beckett.Messages;
using Beckett.Subscriptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Beckett.Tests.Subscriptions;

public class SubscriptionHandlerTests
{
    private readonly Subscription _subscription = new(new SubscriptionGroup("test"), "test");
    private readonly IServiceProvider _serviceProvider;
    private readonly ITestService _testService = new TestService();
    private readonly TestResultHandler _testResultHandler = new();
    private readonly FakeLogger<SubscriptionHandler> _logger = new();

    public SubscriptionHandlerTests()
    {
        _serviceProvider = new ServiceCollection()
            .AddSingleton(_testService)
            .AddSingleton<IResultHandler<TestHandlerResult>>(_testResultHandler)
            .BuildServiceProvider();
    }

    [Fact]
    public async Task supports_message_context_handler()
    {
        _subscription.RegisterMessageType<TestMessage>();
        var handler = new SubscriptionHandler(_subscription, MessageContextHandler.Handle);
        var messageContext = BuildMessageContext();
        var subscriptionContext = BuildSubscriptionContext();

        await handler.Invoke(messageContext, subscriptionContext, _serviceProvider, _logger, CancellationToken.None);

        Assert.False(handler.IsBatchHandler);
        Assert.NotNull(MessageContextHandler.ReceivedContext);
        Assert.Equal(messageContext, MessageContextHandler.ReceivedContext);
    }

    [Fact]
    public async Task supports_handler_that_expects_subscription_context()
    {
        _subscription.RegisterMessageType<TestMessage>();
        var handler = new SubscriptionHandler(_subscription, SubscriptionContextHandler.Handle);
        var messageContext = BuildMessageContext();
        var subscriptionContext = BuildSubscriptionContext();

        await handler.Invoke(messageContext, subscriptionContext, _serviceProvider, _logger, CancellationToken.None);

        Assert.False(handler.IsBatchHandler);
        Assert.NotNull(SubscriptionContextHandler.ReceivedContext);
        Assert.Equal(subscriptionContext, SubscriptionContextHandler.ReceivedContext);
    }

    [Fact]
    public async Task supports_message_handler()
    {
        _subscription.RegisterMessageType<TestMessage>();
        var handler = new SubscriptionHandler(_subscription, MessageHandler.Handle);
        var message = new TestMessage(Guid.NewGuid());
        var messageContext = BuildMessageContext(message);
        var subscriptionContext = BuildSubscriptionContext();

        await handler.Invoke(messageContext, subscriptionContext, _serviceProvider, _logger, CancellationToken.None);

        Assert.False(handler.IsBatchHandler);
        Assert.NotNull(MessageHandler.ReceivedMessage);
        Assert.Equal(message, MessageHandler.ReceivedMessage);
    }

    [Fact]
    public async Task supports_typed_message_context_handler()
    {
        _subscription.RegisterMessageType<TestMessage>();
        var handler = new SubscriptionHandler(_subscription, TypedMessageContextHandler.Handle);
        var message = new TestMessage(Guid.NewGuid());
        var context = BuildMessageContext(message);
        var typedContext = new MessageContext<TestMessage>(context);
        var subscriptionContext = BuildSubscriptionContext();

        await handler.Invoke(typedContext, subscriptionContext, _serviceProvider, _logger, CancellationToken.None);

        Assert.False(handler.IsBatchHandler);
        Assert.NotNull(TypedMessageContextHandler.ReceivedMessage);
        Assert.Equal(message, TypedMessageContextHandler.ReceivedMessage);
    }

    [Fact]
    public async Task supports_message_and_context_handler()
    {
        _subscription.RegisterMessageType<TestMessage>();
        var handler = new SubscriptionHandler(_subscription, MessageAndContextHandler.Handle);
        var message = new TestMessage(Guid.NewGuid());
        var messageContext = BuildMessageContext(message);
        var subscriptionContext = BuildSubscriptionContext();

        await handler.Invoke(messageContext, subscriptionContext, _serviceProvider, _logger, CancellationToken.None);

        Assert.False(handler.IsBatchHandler);
        Assert.NotNull(MessageAndContextHandler.ReceivedMessage);
        Assert.NotNull(MessageAndContextHandler.ReceivedContext);
        Assert.Equal(message, MessageAndContextHandler.ReceivedMessage);
        Assert.Equal(messageContext, MessageAndContextHandler.ReceivedContext);
    }

    [Fact]
    public async Task supports_batch_handler()
    {
        _subscription.RegisterMessageType<TestMessage>();
        var handler = new SubscriptionHandler(_subscription, BatchHandler.Handle);
        var batch = BuildMessageBatch();
        var subscriptionContext = BuildSubscriptionContext();

        await handler.Invoke(batch, subscriptionContext, _serviceProvider, _logger, CancellationToken.None);

        Assert.True(handler.IsBatchHandler);
        Assert.NotNull(BatchHandler.ReceivedBatch);
        Assert.Equal(batch, BatchHandler.ReceivedBatch);
    }

    [Fact]
    public async Task supports_typed_batch_handler()
    {
        _subscription.RegisterMessageType<TestMessage>();
        var handler = new SubscriptionHandler(_subscription, TypedBatchHandler.Handle);
        var batch = BuildMessageBatch();
        var subscriptionContext = BuildSubscriptionContext();
        var expectedBatch = batch.Select(x => x.Message).Cast<TestMessage>().ToList();

        await handler.Invoke(batch, subscriptionContext, _serviceProvider, _logger, CancellationToken.None);

        Assert.True(handler.IsBatchHandler);
        Assert.NotNull(TypedBatchHandler.ReceivedBatch);
        Assert.Equal(expectedBatch, TypedBatchHandler.ReceivedBatch);
    }

    [Fact]
    public async Task supports_unwrapped_batch_handler()
    {
        _subscription.RegisterMessageType<TestMessage>();
        var handler = new SubscriptionHandler(_subscription, UnwrappedBatchHandler.Handle);
        var batch = BuildMessageBatch();
        var subscriptionContext = BuildSubscriptionContext();
        var expectedBatch = batch.Select(x => x.Message!).ToList();

        await handler.Invoke(batch, subscriptionContext, _serviceProvider, _logger, CancellationToken.None);

        Assert.True(handler.IsBatchHandler);
        Assert.NotNull(UnwrappedBatchHandler.ReceivedBatch);
        Assert.Equal(expectedBatch, UnwrappedBatchHandler.ReceivedBatch);
    }

    [Fact]
    public async Task supports_handler_with_dependencies()
    {
        _subscription.RegisterMessageType<TestMessage>();
        var handler = new SubscriptionHandler(_subscription, HandlerWithDependencies.Handle);
        var messageContext = BuildMessageContext();
        var subscriptionContext = BuildSubscriptionContext();

        await handler.Invoke(messageContext, subscriptionContext, _serviceProvider, _logger, CancellationToken.None);

        Assert.True(_testService.ExecuteCalled);
    }

    [Fact]
    public async Task supports_inline_handlers()
    {
        _subscription.RegisterMessageType<TestMessage>();
        TestMessage? receivedMessage = null;
        var handler = new SubscriptionHandler(
            _subscription,
            (TestMessage message, CancellationToken _) =>
            {
                receivedMessage = message;

                return Task.CompletedTask;
            }
        );
        var message = new TestMessage(Guid.NewGuid());
        var messageContext = BuildMessageContext(message);
        var subscriptionContext = BuildSubscriptionContext();

        await handler.Invoke(messageContext, subscriptionContext, _serviceProvider, _logger, CancellationToken.None);

        Assert.Equal(message, receivedMessage);
    }

    [Fact]
    public void handlers_can_subscribe_to_categories_without_specifying_message_types()
    {
        _subscription.Category = "test";

        try
        {
            _ = new SubscriptionHandler(_subscription, MessageContextHandler.Handle);
        }
        catch
        {
            Assert.Fail("Category-only subscription should be valid");
        }
    }

    [Fact]
    public void handlers_can_subscribe_to_a_stream_without_specifying_message_types()
    {
        _subscription.StreamName = "test";

        try
        {
            _ = new SubscriptionHandler(_subscription, MessageContextHandler.Handle);
        }
        catch
        {
            Assert.Fail("Stream-only subscription should be valid");
        }
    }

    [Fact]
    public void handlers_can_return_void()
    {
        _subscription.RegisterMessageType<TestMessage>();

        try
        {
            _ = new SubscriptionHandler(_subscription, HandlerThatReturnsVoid.Handle);
        }
        catch
        {
            Assert.Fail("Handlers should be able to return void");
        }
    }

    [Fact]
    public async Task handlers_use_registered_result_handlers_when_non_empty_result_is_returned()
    {
        _subscription.RegisterMessageType<TestMessage>();
        var handler = new SubscriptionHandler(_subscription, HandlerThatReturnsResult.Handle);
        var messageContext = BuildMessageContext(new TestMessage(Guid.NewGuid()));
        var subscriptionContext = BuildSubscriptionContext();

        await handler.Invoke(messageContext, subscriptionContext, _serviceProvider, _logger, CancellationToken.None);

        Assert.Equal(100, _testResultHandler.Value);
    }

    [Fact]
    public async Task logs_trace_level_warning_when_handler_returns_result_with_no_registered_handler()
    {
        _subscription.RegisterMessageType<TestMessage>();
        var handler = new SubscriptionHandler(_subscription, HandlerThatReturnsResultWithNoHandler.Handle);
        var messageContext = BuildMessageContext(new TestMessage(Guid.NewGuid()));
        var subscriptionContext = BuildSubscriptionContext();

        await handler.Invoke(messageContext, subscriptionContext, _serviceProvider, _logger, CancellationToken.None);

        Assert.Equal(LogLevel.Trace, _logger.LatestRecord.Level);
        Assert.Contains("was not registered in the container", _logger.LatestRecord.Message);
    }

    [Fact]
    public void handlers_must_subscribe_to_at_least_one_message_type_if_category_or_stream_not_specified()
    {
        Assert.Throws<InvalidOperationException>(() => new SubscriptionHandler(
                _subscription,
                MessageContextHandler.Handle
            )
        );
    }

    [Fact]
    public void handlers_can_only_accept_messages_or_batches_not_both()
    {
        Assert.Throws<InvalidOperationException>(() => new SubscriptionHandler(
                _subscription,
                InvalidHandlerWithContextAndBatch.Handle
            )
        );
    }

    [Fact]
    public void typed_handlers_cannot_accept_multiple_message_types()
    {
        _subscription.RegisterMessageType<TestMessage>();
        _subscription.RegisterMessageType<AnotherTestMessage>();

        Assert.Throws<InvalidOperationException>(() => new SubscriptionHandler(_subscription, MessageHandler.Handle));
    }

    [Fact]
    public void typed_batch_handlers_cannot_accept_multiple_message_types()
    {
        _subscription.RegisterMessageType<TestMessage>();
        _subscription.RegisterMessageType<AnotherTestMessage>();

        Assert.Throws<InvalidOperationException>(() => new SubscriptionHandler(
                _subscription,
                TypedBatchHandler.Handle
            )
        );
    }

    [Fact]
    public void batch_handlers_cannot_also_handle_messages()
    {
        _subscription.RegisterMessageType<TestMessage>();

        Assert.Throws<InvalidOperationException>(() => new SubscriptionHandler(
                _subscription,
                InvalidHandlerWithBatchAndMessage.Handle
            )
        );
    }

    [Fact]
    public void handlers_must_handle_something()
    {
        Assert.Throws<InvalidOperationException>(() => new SubscriptionHandler(
                _subscription,
                InvalidHandlerThatHandlesNothing.Handle
            )
        );
    }

    [Fact]
    public void handlers_must_return_task_or_void_only()
    {
        Assert.Throws<InvalidOperationException>(() => new SubscriptionHandler(
                _subscription,
                InvalidHandlerThatReturnsNonTaskOrVoid.Handle
            )
        );
    }

    [Fact]
    public void sets_handler_name_for_static_class_methods()
    {
        var expectedHandlerName = $"{typeof(MessageHandler).FullName}::{nameof(MessageHandler.Handle)}";
        _subscription.RegisterMessageType<TestMessage>();

        _ = new SubscriptionHandler(_subscription, MessageHandler.Handle);

        Assert.Equal(expectedHandlerName, _subscription.HandlerName);
    }

    [Fact]
    public void does_not_set_handler_name_for_non_static_methods()
    {
        _subscription.RegisterMessageType<TestMessage>();

        _ = new SubscriptionHandler(_subscription, (IMessageContext _, CancellationToken _) => Task.CompletedTask);

        Assert.Null(_subscription.HandlerName);
    }

    [Fact]
    public void does_not_override_handler_name_if_already_set()
    {
        _subscription.RegisterMessageType<TestMessage>();
        _subscription.HandlerName = "test";

        _ = new SubscriptionHandler(_subscription, MessageHandler.Handle);

        Assert.Equal("test", _subscription.HandlerName);
    }

    private static ISubscriptionContext BuildSubscriptionContext() =>
        new SubscriptionContext(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), SubscriptionStatus.Active);

    private static MessageContext BuildMessageContext(TestMessage? message = null) => new(
        Guid.NewGuid().ToString(),
        "test-123",
        1,
        1,
        "test-message",
        message != null ? MessageSerializer.Serialize(typeof(TestMessage), message) : EmptyJsonElement.Instance,
        EmptyJsonElement.Instance,
        DateTimeOffset.UtcNow
    );

    private static IReadOnlyList<IMessageContext> BuildMessageBatch() =>
    [
        new MessageContext(
            Guid.NewGuid().ToString(),
            "test-123",
            1,
            1,
            "test-message",
            EmptyJsonElement.Instance,
            EmptyJsonElement.Instance,
            DateTimeOffset.UtcNow
        )
    ];

    private static class MessageContextHandler
    {
        public static IMessageContext? ReceivedContext { get; private set; }

        public static Task Handle(IMessageContext context, CancellationToken cancellationToken)
        {
            ReceivedContext = context;

            return Task.CompletedTask;
        }
    }

    private static class SubscriptionContextHandler
    {
        public static ISubscriptionContext? ReceivedContext { get; private set; }

        public static Task Handle(IMessageContext _, ISubscriptionContext subscriptionContext)
        {
            ReceivedContext = subscriptionContext;

            return Task.CompletedTask;
        }
    }

    private static class MessageHandler
    {
        public static TestMessage? ReceivedMessage { get; private set; }

        public static Task Handle(TestMessage message, CancellationToken cancellationToken)
        {
            ReceivedMessage = message;

            return Task.CompletedTask;
        }
    }

    private static class TypedMessageContextHandler
    {
        public static TestMessage? ReceivedMessage { get; private set; }

        public static Task Handle(IMessageContext<TestMessage> context, CancellationToken cancellationToken)
        {
            ReceivedMessage = context.Message;

            return Task.CompletedTask;
        }
    }

    private static class MessageAndContextHandler
    {
        public static TestMessage? ReceivedMessage { get; private set; }
        public static IMessageContext? ReceivedContext { get; private set; }

        public static Task Handle(TestMessage message, IMessageContext context, CancellationToken cancellationToken)
        {
            ReceivedMessage = message;
            ReceivedContext = context;

            return Task.CompletedTask;
        }
    }

    private static class BatchHandler
    {
        public static IReadOnlyList<IMessageContext>? ReceivedBatch { get; private set; }

        public static Task Handle(IReadOnlyList<IMessageContext> batch, CancellationToken cancellationToken)
        {
            ReceivedBatch = batch;

            return Task.CompletedTask;
        }
    }

    private static class TypedBatchHandler
    {
        public static IReadOnlyList<TestMessage>? ReceivedBatch { get; private set; }

        public static Task Handle(IReadOnlyList<TestMessage> batch, CancellationToken cancellationToken)
        {
            ReceivedBatch = batch;

            return Task.CompletedTask;
        }
    }

    private static class UnwrappedBatchHandler
    {
        public static IReadOnlyList<object>? ReceivedBatch { get; private set; }

        public static Task Handle(IReadOnlyList<object> batch, CancellationToken cancellationToken)
        {
            ReceivedBatch = batch;

            return Task.CompletedTask;
        }
    }

    private static class HandlerWithDependencies
    {
        public static Task Handle(IMessageContext context, ITestService service, CancellationToken cancellationToken)
        {
            return service.Execute(cancellationToken);
        }
    }

    private static class HandlerThatReturnsResult
    {
        public static Task<TestHandlerResult> Handle(IMessageContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(new TestHandlerResult(100));
        }
    }

    private static class HandlerThatReturnsResultWithNoHandler
    {
        public static Task<int> Handle(IMessageContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(100);
        }
    }

    private record TestHandlerResult(int Value);

    private class TestResultHandler : IResultHandler<TestHandlerResult>
    {
        public int Value { get; private set; }

        public Task Handle(TestHandlerResult result, CancellationToken cancellationToken)
        {
            Value = result.Value;

            return Task.CompletedTask;
        }
    }

    private static class HandlerThatReturnsVoid
    {
        public static void Handle(IMessageContext context)
        {
        }
    }

    private static class InvalidHandlerWithContextAndBatch
    {
        public static Task Handle(
            IMessageContext context,
            IReadOnlyList<IMessageContext> batch,
            CancellationToken cancellationToken
        )
        {
            return Task.CompletedTask;
        }
    }

    private static class InvalidHandlerWithBatchAndMessage
    {
        public static Task Handle(
            IReadOnlyList<IMessageContext> batch,
            TestMessage message,
            CancellationToken cancellationToken
        )
        {
            return Task.CompletedTask;
        }
    }

    private static class InvalidHandlerThatHandlesNothing
    {
        public static Task Handle(ITestService service, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private static class InvalidHandlerThatReturnsNonTaskOrVoid
    {
        public static int Handle(IMessageContext context) => 1;
    }

    private interface ITestService
    {
        bool ExecuteCalled { get; }

        Task Execute(CancellationToken cancellationToken);
    }

    private class TestService : ITestService
    {
        public bool ExecuteCalled { get; private set; }

        public Task Execute(CancellationToken cancellationToken)
        {
            ExecuteCalled = true;

            return Task.CompletedTask;
        }
    }
}
