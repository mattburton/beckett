using System.Text.Json;
using Beckett.Messages;
using Beckett.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Tests.Subscriptions;

[Collection("MessageTypeMap")]
public class SubscriptionHandlerTests : IDisposable
{
    private readonly Subscription _subscription = new("test");
    private readonly IServiceProvider _serviceProvider;
    private readonly ITestService _testService = new TestService();

    public SubscriptionHandlerTests()
    {
        MessageTypeMap.Map<TestMessage>("test-message");
        MessageTypeMap.Map<AnotherTestMessage>("another-test-message");

        _serviceProvider = new ServiceCollection()
            .AddSingleton(_testService)
            .BuildServiceProvider();
    }

    [Fact]
    public void supports_message_context_handler()
    {
        _subscription.RegisterMessageType<TestMessage>();
        var handler = new SubscriptionHandler(_subscription, MessageContextHandler.Handle);
        var context = BuildMessageContext();

        handler.Invoke(context, _serviceProvider, CancellationToken.None);

        Assert.False(handler.IsBatchHandler);
        Assert.NotNull(MessageContextHandler.ReceivedContext);
        Assert.Equal(context, MessageContextHandler.ReceivedContext);
    }

    [Fact]
    public void supports_message_handler()
    {
        _subscription.RegisterMessageType<TestMessage>();
        var handler = new SubscriptionHandler(_subscription, MessageHandler.Handle);
        var message = new TestMessage(Guid.NewGuid());
        var context = BuildMessageContext(message);

        handler.Invoke(context, _serviceProvider, CancellationToken.None);

        Assert.False(handler.IsBatchHandler);
        Assert.NotNull(MessageHandler.ReceivedMessage);
        Assert.Equal(message, MessageHandler.ReceivedMessage);
    }

    [Fact]
    public void supports_message_and_context_handler()
    {
        _subscription.RegisterMessageType<TestMessage>();
        var handler = new SubscriptionHandler(_subscription, MessageAndContextHandler.Handle);
        var message = new TestMessage(Guid.NewGuid());
        var context = BuildMessageContext(message);

        handler.Invoke(context, _serviceProvider, CancellationToken.None);

        Assert.False(handler.IsBatchHandler);
        Assert.NotNull(MessageAndContextHandler.ReceivedMessage);
        Assert.NotNull(MessageAndContextHandler.ReceivedContext);
        Assert.Equal(message, MessageAndContextHandler.ReceivedMessage);
        Assert.Equal(context, MessageAndContextHandler.ReceivedContext);
    }

    [Fact]
    public void supports_message_batch_handler()
    {
        _subscription.RegisterMessageType<TestMessage>();
        var handler = new SubscriptionHandler(_subscription, MessageBatchHandler.Handle);
        var batch = BuildMessageBatch();

        handler.Invoke(batch, _serviceProvider, CancellationToken.None);

        Assert.True(handler.IsBatchHandler);
        Assert.NotNull(MessageBatchHandler.ReceivedBatch);
        Assert.Equal(batch, MessageBatchHandler.ReceivedBatch);
    }

    [Fact]
    public void supports_handler_with_dependencies()
    {
        _subscription.RegisterMessageType<TestMessage>();
        var handler = new SubscriptionHandler(_subscription, HandlerWithDependencies.Handle);
        var context = BuildMessageContext();

        handler.Invoke(context, _serviceProvider, CancellationToken.None);

        Assert.True(_testService.ExecuteCalled);
    }

    [Fact]
    public void supports_inline_handlers()
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
        var context = BuildMessageContext(message);

        handler.Invoke(context, _serviceProvider, CancellationToken.None);

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
    public void handlers_must_subscribe_to_at_least_one_message_type_if_category_not_specified()
    {
        Assert.Throws<InvalidOperationException>(
            () => new SubscriptionHandler(_subscription, MessageContextHandler.Handle)
        );
    }

    [Fact]
    public void handlers_can_only_accept_messages_or_batches_not_both()
    {
        Assert.Throws<InvalidOperationException>(
            () => new SubscriptionHandler(_subscription, InvalidHandlerWithContextAndBatch.Handle)
        );
    }

    [Fact]
    public void typed_handlers_cannot_accept_multiple_message_types()
    {
        _subscription.RegisterMessageType<TestMessage>();
        _subscription.RegisterMessageType<AnotherTestMessage>();

        Assert.Throws<InvalidOperationException>(
            () => new SubscriptionHandler(_subscription, MessageHandler.Handle)
        );
    }

    [Fact]
    public void batch_handlers_cannot_also_handle_messages()
    {
        _subscription.RegisterMessageType<TestMessage>();

        Assert.Throws<InvalidOperationException>(
            () => new SubscriptionHandler(_subscription, InvalidHandlerWithBatchAndMessage.Handle)
        );
    }

    [Fact]
    public void handlers_must_handle_something()
    {
        Assert.Throws<InvalidOperationException>(
            () => new SubscriptionHandler(_subscription, InvalidHandlerThatHandlesNothing.Handle)
        );
    }

    [Fact]
    public void handlers_must_return_task()
    {
        Assert.Throws<InvalidOperationException>(
            () => new SubscriptionHandler(_subscription, InvalidHandlerThatReturnsVoid.Handle)
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

    private static MessageContext BuildMessageContext(TestMessage? message = null) => new(
        Guid.NewGuid().ToString(),
        "test-123",
        1,
        1,
        "test-message",
        message != null ? MessageSerializer.Serialize(typeof(TestMessage), message) : JsonDocument.Parse("{}"),
        JsonDocument.Parse("{}"),
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
            JsonDocument.Parse("{}"),
            JsonDocument.Parse("{}"),
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

    private static class MessageHandler
    {
        public static TestMessage? ReceivedMessage { get; private set; }

        public static Task Handle(TestMessage message, CancellationToken cancellationToken)
        {
            ReceivedMessage = message;

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

    private static class MessageBatchHandler
    {
        public static IReadOnlyList<IMessageContext>? ReceivedBatch { get; private set; }

        public static Task Handle(IReadOnlyList<IMessageContext> batch, CancellationToken cancellationToken)
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

    private static class InvalidHandlerWithContextAndBatch
    {
        public static Task Handle(IMessageContext context, IReadOnlyList<IMessageContext> batch, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private static class InvalidHandlerWithBatchAndMessage
    {
        public static Task Handle(IReadOnlyList<IMessageContext> batch, TestMessage message, CancellationToken cancellationToken)
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

    private static class InvalidHandlerThatReturnsVoid
    {
        public static void Handle(IMessageContext context)
        {
        }
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

    private record TestMessage(Guid Id);

    private record AnotherTestMessage(Guid Id);

    public void Dispose()
    {
        MessageTypeMap.Clear();
    }
}
