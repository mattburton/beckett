using System.Text.Json;
using Beckett.Database;
using Beckett.Messages;
using Beckett.OpenTelemetry;
using Beckett.Subscriptions;
using Beckett.Subscriptions.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Beckett.Tests.Subscriptions;

public class CheckpointProcessorTests
{
    public class when_checkpoint_is_active
    {
        public class when_messages_to_process_is_less_than_batch_size
        {
            [Fact]
            public async Task only_reads_messages_up_to_stream_version()
            {
                var checkpoint = new Checkpoint(1, "test", "test", "test", 0, 10, 0, CheckpointStatus.Active);
                var subscription = new Subscription("test")
                {
                    HandlerDelegate = (IMessageContext _) => { }
                };
                subscription.RegisterMessageType<TestMessage>();
                subscription.BuildHandler();
                var options = new BeckettOptions
                {
                    Subscriptions =
                    {
                        SubscriptionStreamBatchSize = 10
                    }
                };
                var messageStore = Substitute.For<IMessageStore>();
                var checkpointProcessor = BuildCheckpointProcessor(options, messageStore);

                await checkpointProcessor.Process(1, checkpoint, subscription, CancellationToken.None);

                await messageStore.Received().ReadStream(
                    "test",
                    Arg.Is<ReadOptions>(x => x.StartingStreamPosition == 1 && x.EndingStreamPosition == 10),
                    CancellationToken.None
                );
            }
        }

        public class when_messages_to_process_exceeds_batch_size
        {
            [Fact]
            public async Task only_reads_messages_up_to_batch_size()
            {
                var checkpoint = new Checkpoint(1, "test", "test", "test", 0, 20, 0, CheckpointStatus.Active);
                var subscription = new Subscription("test")
                {
                    HandlerDelegate = (IMessageContext _) => { }
                };
                subscription.RegisterMessageType<TestMessage>();
                subscription.BuildHandler();
                var options = new BeckettOptions
                {
                    Subscriptions =
                    {
                        SubscriptionStreamBatchSize = 10
                    }
                };
                var messageStore = Substitute.For<IMessageStore>();
                var checkpointProcessor = BuildCheckpointProcessor(options, messageStore);

                await checkpointProcessor.Process(1, checkpoint, subscription, CancellationToken.None);

                await messageStore.Received().ReadStream(
                    "test",
                    Arg.Is<ReadOptions>(x => x.StartingStreamPosition == 1 && x.EndingStreamPosition == 10),
                    CancellationToken.None
                );
            }
        }
    }

    public class when_subscription_handler_is_message_handler
    {
        public class when_reservation_timeout_is_exceeded
        {
            [Fact]
            public async Task throws_timeout_exception()
            {
                var checkpoint = new Checkpoint(1, "test", "test", "test", 1, 2, 0, CheckpointStatus.Active);
                var subscription = new Subscription("test")
                {
                    HandlerDelegate = async (IMessageContext _, CancellationToken ct) =>
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(2), ct);
                    }
                };
                subscription.RegisterMessageType<TestMessage>();
                subscription.BuildHandler();
                var options = new BeckettOptions
                {
                    Subscriptions =
                    {
                        ReservationTimeout = TimeSpan.FromMilliseconds(1)
                    }
                };
                var messageStore = Substitute.For<IMessageStore>();
                var database = Substitute.For<IPostgresDatabase>();
                var checkpointProcessor = BuildCheckpointProcessor(options, messageStore, database);
                messageStore.ReadStream("test", Arg.Any<ReadOptions>(), CancellationToken.None)
                    .Returns(new MessageStream("test", 2, [BuildMessageContext()], messageStore.AppendToStream));
                RecordCheckpointError? error = null;
                database.WhenForAnyArgs(x => x.Execute(Arg.Any<RecordCheckpointError>(), Arg.Any<CancellationToken>()))
                    .Do(x => error = x.Arg<RecordCheckpointError>());

                await checkpointProcessor.Process(1, checkpoint, subscription, CancellationToken.None);

                Assert.NotNull(error);
                Assert.True(IsTimeoutException(error));
            }
        }

        public class when_nested_timeout_exception_is_thrown
        {
            [Fact]
            public async Task throws_timeout_exception()
            {
                var checkpoint = new Checkpoint(1, "test", "test", "test", 1, 2, 0, CheckpointStatus.Active);
                var subscription = new Subscription("test")
                {
                    HandlerDelegate = (IMessageContext _, CancellationToken ct) =>
                    {
                        throw new TaskCanceledException("test", new TimeoutException("timeout"), ct);
                    }
                };
                subscription.RegisterMessageType<TestMessage>();
                subscription.BuildHandler();
                var options = new BeckettOptions
                {
                    Subscriptions =
                    {
                        ReservationTimeout = TimeSpan.FromMilliseconds(1)
                    }
                };
                var messageStore = Substitute.For<IMessageStore>();
                var database = Substitute.For<IPostgresDatabase>();
                var checkpointProcessor = BuildCheckpointProcessor(options, messageStore, database);
                messageStore.ReadStream("test", Arg.Any<ReadOptions>(), CancellationToken.None)
                    .Returns(new MessageStream("test", 2, [BuildMessageContext()], messageStore.AppendToStream));
                RecordCheckpointError? error = null;
                database.WhenForAnyArgs(x => x.Execute(Arg.Any<RecordCheckpointError>(), Arg.Any<CancellationToken>()))
                    .Do(x => error = x.Arg<RecordCheckpointError>());

                await checkpointProcessor.Process(1, checkpoint, subscription, CancellationToken.None);

                Assert.NotNull(error);
                Assert.True(IsTimeoutException(error));
            }
        }

        public class when_the_stopping_token_is_cancelled
        {
            [Fact]
            public async Task throws_operation_canceled_exception()
            {
                var checkpoint = new Checkpoint(1, "test", "test", "test", 1, 2, 0, CheckpointStatus.Active);
                var subscription = new Subscription("test")
                {
                    HandlerDelegate = (IMessageContext _, CancellationToken ct) =>
                        Task.Delay(TimeSpan.FromMilliseconds(2), ct)
                };
                subscription.RegisterMessageType<TestMessage>();
                subscription.BuildHandler();
                var options = new BeckettOptions
                {
                    Subscriptions =
                    {
                        ReservationTimeout = TimeSpan.FromMilliseconds(1)
                    }
                };
                var messageStore = Substitute.For<IMessageStore>();
                var checkpointProcessor = BuildCheckpointProcessor(options, messageStore);
                var parentCts = new CancellationTokenSource();
                var innerCts = CancellationTokenSource.CreateLinkedTokenSource(parentCts.Token);

                messageStore.ReadStream("test", Arg.Any<ReadOptions>(), Arg.Any<CancellationToken>())
                    .Returns(new MessageStream("test", 2, [BuildMessageContext()], messageStore.AppendToStream));
                await parentCts.CancelAsync();

                await Assert.ThrowsAsync<OperationCanceledException>(() => checkpointProcessor.Process(1, checkpoint, subscription, innerCts.Token));
            }
        }

        public class when_checkpoint_is_retry_or_failure
        {
            [Fact]
            public async Task only_reads_one_message_to_retry()
            {
                var checkpoint = new Checkpoint(1, "test", "test", "test", 1, 20, 0, CheckpointStatus.Retry);
                var subscription = new Subscription("test")
                {
                    HandlerDelegate = (IMessageContext _) => { }
                };
                subscription.RegisterMessageType<TestMessage>();
                subscription.BuildHandler();
                var options = new BeckettOptions
                {
                    Subscriptions =
                    {
                        SubscriptionStreamBatchSize = 10
                    }
                };
                var messageStore = Substitute.For<IMessageStore>();
                var checkpointProcessor = BuildCheckpointProcessor(options, messageStore);

                await checkpointProcessor.Process(1, checkpoint, subscription, CancellationToken.None);

                await messageStore.Received().ReadStream(
                    "test",
                    Arg.Is<ReadOptions>(x => x.StartingStreamPosition == 1 && x.EndingStreamPosition == 2),
                    CancellationToken.None
                );
            }
        }
    }

    public class when_subscription_handler_is_batch_handler
    {
        public class when_reservation_timeout_is_exceeded
        {
            [Fact]
            public async Task throws_timeout_exception()
            {
                var checkpoint = new Checkpoint(1, "test", "test", "test", 1, 2, 0, CheckpointStatus.Active);
                var subscription = new Subscription("test")
                {
                    HandlerDelegate = async (IReadOnlyList<IMessageContext> _, CancellationToken ct) =>
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(2), ct);
                    }
                };
                subscription.RegisterMessageType<TestMessage>();
                subscription.BuildHandler();
                var options = new BeckettOptions
                {
                    Subscriptions =
                    {
                        ReservationTimeout = TimeSpan.FromMilliseconds(1)
                    }
                };
                var messageStore = Substitute.For<IMessageStore>();
                var database = Substitute.For<IPostgresDatabase>();
                var checkpointProcessor = BuildCheckpointProcessor(options, messageStore, database);
                messageStore.ReadStream("test", Arg.Any<ReadOptions>(), CancellationToken.None)
                    .Returns(new MessageStream("test", 2, [BuildMessageContext()], messageStore.AppendToStream));
                RecordCheckpointError? error = null;
                database.WhenForAnyArgs(x => x.Execute(Arg.Any<RecordCheckpointError>(), Arg.Any<CancellationToken>()))
                    .Do(x => error = x.Arg<RecordCheckpointError>());

                await checkpointProcessor.Process(1, checkpoint, subscription, CancellationToken.None);

                Assert.NotNull(error);
                Assert.True(IsTimeoutException(error));
            }
        }

        public class when_nested_timeout_exception_is_thrown
        {
            [Fact]
            public async Task throws_timeout_exception()
            {
                var checkpoint = new Checkpoint(1, "test", "test", "test", 1, 2, 0, CheckpointStatus.Active);
                var subscription = new Subscription("test")
                {
                    HandlerDelegate = (IReadOnlyList<IMessageContext> _, CancellationToken ct) =>
                    {
                        throw new TaskCanceledException("test", new TimeoutException("timeout"), ct);
                    }
                };
                subscription.RegisterMessageType<TestMessage>();
                subscription.BuildHandler();
                var options = new BeckettOptions
                {
                    Subscriptions =
                    {
                        ReservationTimeout = TimeSpan.FromMilliseconds(1)
                    }
                };
                var messageStore = Substitute.For<IMessageStore>();
                var database = Substitute.For<IPostgresDatabase>();
                var checkpointProcessor = BuildCheckpointProcessor(options, messageStore, database);
                messageStore.ReadStream("test", Arg.Any<ReadOptions>(), CancellationToken.None)
                    .Returns(new MessageStream("test", 2, [BuildMessageContext()], messageStore.AppendToStream));
                RecordCheckpointError? error = null;
                database.WhenForAnyArgs(x => x.Execute(Arg.Any<RecordCheckpointError>(), Arg.Any<CancellationToken>()))
                    .Do(x => error = x.Arg<RecordCheckpointError>());

                await checkpointProcessor.Process(1, checkpoint, subscription, CancellationToken.None);

                Assert.NotNull(error);
                Assert.True(IsTimeoutException(error));
            }
        }

        public class when_the_stopping_token_is_cancelled
        {
            [Fact]
            public async Task throws_operation_canceled_exception()
            {
                var checkpoint = new Checkpoint(1, "test", "test", "test", 1, 2, 0, CheckpointStatus.Active);
                var subscription = new Subscription("test")
                {
                    HandlerDelegate = (IReadOnlyList<IMessageContext> _, CancellationToken ct) =>
                        Task.Delay(TimeSpan.FromMilliseconds(2), ct)
                };
                subscription.RegisterMessageType<TestMessage>();
                subscription.BuildHandler();
                var options = new BeckettOptions
                {
                    Subscriptions =
                    {
                        ReservationTimeout = TimeSpan.FromMilliseconds(1)
                    }
                };
                var messageStore = Substitute.For<IMessageStore>();
                var checkpointProcessor = BuildCheckpointProcessor(options, messageStore);
                var parentCts = new CancellationTokenSource();
                var innerCts = CancellationTokenSource.CreateLinkedTokenSource(parentCts.Token);

                messageStore.ReadStream("test", Arg.Any<ReadOptions>(), Arg.Any<CancellationToken>())
                    .Returns(new MessageStream("test", 2, [BuildMessageContext()], messageStore.AppendToStream));
                await parentCts.CancelAsync();

                await Assert.ThrowsAsync<OperationCanceledException>(() => checkpointProcessor.Process(1, checkpoint, subscription, innerCts.Token));
            }
        }

        public class when_checkpoint_is_retry_or_failure
        {
            public class when_messages_to_process_is_less_than_batch_size
            {
                [Fact]
                public async Task only_reads_messages_up_to_stream_version()
                {
                    var checkpoint = new Checkpoint(1, "test", "test", "test", 1, 10, 0, CheckpointStatus.Retry);
                    var subscription = new Subscription("test")
                    {
                        HandlerDelegate = (IReadOnlyList<IMessageContext> _) => { }
                    };
                    subscription.RegisterMessageType<TestMessage>();
                    subscription.BuildHandler();
                    var options = new BeckettOptions
                    {
                        Subscriptions =
                        {
                            SubscriptionStreamBatchSize = 10
                        }
                    };
                    var messageStore = Substitute.For<IMessageStore>();
                    var checkpointProcessor = BuildCheckpointProcessor(options, messageStore);

                    await checkpointProcessor.Process(1, checkpoint, subscription, CancellationToken.None);

                    await messageStore.Received().ReadStream(
                        "test",
                        Arg.Is<ReadOptions>(x => x.StartingStreamPosition == 1 && x.EndingStreamPosition == 10),
                        CancellationToken.None
                    );
                }
            }

            public class when_messages_to_process_exceeds_batch_size
            {
                [Fact]
                public async Task only_reads_messages_up_to_batch_size()
                {
                    var checkpoint = new Checkpoint(1, "test", "test", "test", 1, 20, 0, CheckpointStatus.Retry);
                    var subscription = new Subscription("test")
                    {
                        HandlerDelegate = (IReadOnlyList<IMessageContext> _) => { }
                    };
                    subscription.RegisterMessageType<TestMessage>();
                    subscription.BuildHandler();
                    var options = new BeckettOptions
                    {
                        Subscriptions =
                        {
                            SubscriptionStreamBatchSize = 10
                        }
                    };
                    var messageStore = Substitute.For<IMessageStore>();
                    var checkpointProcessor = BuildCheckpointProcessor(options, messageStore);

                    await checkpointProcessor.Process(1, checkpoint, subscription, CancellationToken.None);

                    await messageStore.Received().ReadStream(
                        "test",
                        Arg.Is<ReadOptions>(x => x.StartingStreamPosition == 1 && x.EndingStreamPosition == 11),
                        CancellationToken.None
                    );
                }
            }
        }
    }

    private static MessageContext BuildMessageContext() => new(
        Guid.NewGuid().ToString(),
        "test",
        1,
        1,
        "test-message",
        JsonDocument.Parse("{}"),
        JsonDocument.Parse("{}"),
        DateTimeOffset.UtcNow
    );

    private static bool IsTimeoutException(RecordCheckpointError x)
    {
        if (x.Error.RootElement.TryGetProperty("Type", out var typeProperty))
        {
            return typeProperty.GetString() == typeof(TimeoutException).FullName;
        }

        return false;
    }

    private static CheckpointProcessor BuildCheckpointProcessor(
        BeckettOptions options,
        IMessageStore messageStore,
        IPostgresDatabase? database = null
    )
    {
        database ??= Substitute.For<IPostgresDatabase>();
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var instrumentation = Substitute.For<IInstrumentation>();
        var logger = Substitute.For<ILogger<CheckpointProcessor>>();

        return new CheckpointProcessor(messageStore, database, serviceProvider, options, instrumentation, logger);
    }
}
