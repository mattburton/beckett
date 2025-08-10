using Beckett.Database;
using Beckett.Messages;
using Beckett.OpenTelemetry;
using Beckett.Storage;
using Beckett.Subscriptions;
using Beckett.Subscriptions.Queries;
using Beckett.Subscriptions.Services;
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
                var options = new BeckettOptions();
                var group = options.WithSubscriptionGroup(
                    Guid.NewGuid().ToString(),
                    x => x.SubscriptionBatchSize = 10
                );
                var checkpoint = new Checkpoint(1, 100, "test", 0, 10, 0, CheckpointStatus.Active);
                var subscription = new Subscription(group, "test")
                {
                    HandlerDelegate = (IMessageContext _) => { }
                };
                subscription.RegisterMessageType<TestMessage>();
                subscription.BuildHandler();
                var messageStorage = Substitute.For<IMessageStorage>();
                var checkpointProcessor = BuildCheckpointProcessor(messageStorage);

                await checkpointProcessor.Process(1, checkpoint, subscription);

                await messageStorage.Received().ReadStream(
                    "test",
                    Arg.Is<ReadStreamOptions>(x => x.StartingStreamPosition == 1 && x.EndingStreamPosition == 10),
                    CancellationToken.None
                );
            }
        }

        public class when_messages_to_process_exceeds_batch_size
        {
            [Fact]
            public async Task only_reads_messages_up_to_batch_size()
            {
                var options = new BeckettOptions();
                var group = options.WithSubscriptionGroup(
                    Guid.NewGuid().ToString(),
                    x => x.SubscriptionBatchSize = 10
                );
                var checkpoint = new Checkpoint(1, 100, "test", 0, 20, 0, CheckpointStatus.Active);
                var subscription = new Subscription(group, "test")
                {
                    HandlerDelegate = (IMessageContext _) => { }
                };
                subscription.RegisterMessageType<TestMessage>();
                subscription.BuildHandler();
                var messageStorage = Substitute.For<IMessageStorage>();
                var checkpointProcessor = BuildCheckpointProcessor(messageStorage);

                await checkpointProcessor.Process(1, checkpoint, subscription);

                await messageStorage.Received().ReadStream(
                    "test",
                    Arg.Is<ReadStreamOptions>(x => x.StartingStreamPosition == 1 && x.EndingStreamPosition == 10),
                    CancellationToken.None
                );
            }
        }
    }

    public class when_reservation_timeout_is_exceeded
    {
        [Fact]
        public async Task throws_timeout_exception()
        {
            var options = new BeckettOptions();
            var group = options.WithSubscriptionGroup(
                Guid.NewGuid().ToString(),
                x => x.ReservationTimeout = TimeSpan.FromMilliseconds(1)
            );
            var checkpoint = new Checkpoint(1, 100, "test", 1, 2, 0, CheckpointStatus.Active);
            var subscription = new Subscription(group, "test")
            {
                HandlerDelegate = async (IMessageContext _, CancellationToken ct) =>
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(5), ct);
                }
            };
            subscription.RegisterMessageType<TestMessage>();
            subscription.BuildHandler();
            var messageStorage = Substitute.For<IMessageStorage>();
            var database = Substitute.For<IPostgresDatabase>();
            var checkpointProcessor = BuildCheckpointProcessor(messageStorage, database);
            messageStorage.ReadStream("test", Arg.Any<ReadStreamOptions>(), CancellationToken.None)
                .Returns(new ReadStreamResult("test", 2, [BuildStreamMessage()]));
            RecordCheckpointError? error = null;
            database.WhenForAnyArgs(x => x.Execute(Arg.Any<RecordCheckpointError>(), Arg.Any<CancellationToken>()))
                .Do(x => error = x.Arg<RecordCheckpointError>());

            await checkpointProcessor.Process(1, checkpoint, subscription);

            Assert.NotNull(error);
            Assert.True(IsTimeoutException(error));
        }
    }

    public class when_nested_timeout_exception_is_thrown
    {
        [Fact]
        public async Task throws_timeout_exception()
        {
            var options = new BeckettOptions();
            var group = options.WithSubscriptionGroup(
                Guid.NewGuid().ToString(),
                x => x.ReservationTimeout = TimeSpan.FromMilliseconds(1)
            );
            var checkpoint = new Checkpoint(1, 100, "test", 1, 2, 0, CheckpointStatus.Active);
            var subscription = new Subscription(group, "test")
            {
                HandlerDelegate = (IMessageContext _, CancellationToken ct) =>
                {
                    throw new TaskCanceledException("test", new TimeoutException("timeout"), ct);
                }
            };
            subscription.RegisterMessageType<TestMessage>();
            subscription.BuildHandler();
            var messageStorage = Substitute.For<IMessageStorage>();
            var database = Substitute.For<IPostgresDatabase>();
            var checkpointProcessor = BuildCheckpointProcessor(messageStorage, database);
            messageStorage.ReadStream("test", Arg.Any<ReadStreamOptions>(), CancellationToken.None)
                .Returns(new ReadStreamResult("test", 2, [BuildStreamMessage()]));
            RecordCheckpointError? error = null;
            database.WhenForAnyArgs(x => x.Execute(Arg.Any<RecordCheckpointError>(), Arg.Any<CancellationToken>()))
                .Do(x => error = x.Arg<RecordCheckpointError>());

            await checkpointProcessor.Process(1, checkpoint, subscription);

            Assert.NotNull(error);
            Assert.True(IsTimeoutException(error));
        }
    }

    public class when_checkpoint_is_retry_or_failure
    {
        [Fact]
        public async Task only_reads_one_message_to_retry()
        {
            var options = new BeckettOptions();
            var group = options.WithSubscriptionGroup(
                Guid.NewGuid().ToString(),
                x => x.SubscriptionBatchSize = 10
            );
            var checkpoint = new Checkpoint(1, 100, "test", 1, 20, 0, CheckpointStatus.Retry);
            var subscription = new Subscription(group, "test")
            {
                HandlerDelegate = (IMessageContext _) => { }
            };
            subscription.RegisterMessageType<TestMessage>();
            subscription.BuildHandler();
            var messageStorage = Substitute.For<IMessageStorage>();
            var checkpointProcessor = BuildCheckpointProcessor(messageStorage);

            await checkpointProcessor.Process(1, checkpoint, subscription);

            await messageStorage.Received().ReadStream(
                "test",
                Arg.Is<ReadStreamOptions>(x => x.StartingStreamPosition == 1 && x.EndingStreamPosition == 2),
                CancellationToken.None
            );
        }
    }

    private static StreamMessage BuildStreamMessage() => new(
        Guid.NewGuid().ToString(),
        "test",
        1,
        1,
        "test-message",
        EmptyJsonElement.Instance,
        EmptyJsonElement.Instance,
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
        IMessageStorage messageStorage,
        IPostgresDatabase? database = null
    )
    {
        database ??= Substitute.For<IPostgresDatabase>();
        var dataSource = Substitute.For<IPostgresDataSource>();
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var instrumentation = Substitute.For<IInstrumentation>();
        var logger = Substitute.For<ILogger<CheckpointProcessor>>();

        var mappingService = Substitute.For<ISubscriptionRegistry>();

        return new CheckpointProcessor(
            messageStorage,
            dataSource,
            database,
            serviceProvider,
            mappingService,
            instrumentation,
            logger
        );
    }
}
