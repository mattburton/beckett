using Beckett.Database;
using Beckett.Messages;
using Beckett.OpenTelemetry;
using Beckett.Subscriptions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Beckett.Tests.Subscriptions;

public class CheckpointProcessorTests
{
    public class when_checkpoint_is_active
    {
        public class when_messages_to_process_is_less_than_batch_size : IClassFixture<MessageTypes>
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

        public class when_messages_to_process_exceeds_batch_size : IClassFixture<MessageTypes>
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

    public class when_checkpoint_is_retry_or_failure
    {
        public class when_subscription_handler_is_batch_handler
        {
            public class when_messages_to_process_is_less_than_batch_size : IClassFixture<MessageTypes>
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

            public class when_messages_to_process_exceeds_batch_size : IClassFixture<MessageTypes>
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

        public class when_subscription_handler_is_not_batch_handler : IClassFixture<MessageTypes>
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

    private static CheckpointProcessor BuildCheckpointProcessor(BeckettOptions options, IMessageStore messageStore)
    {
        var database = Substitute.For<IPostgresDatabase>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var instrumentation = Substitute.For<IInstrumentation>();
        var logger = Substitute.For<ILogger<CheckpointProcessor>>();

        return new CheckpointProcessor(messageStore, database, serviceProvider, options, instrumentation, logger);
    }

    public record TestMessage;

    public class MessageTypes : IDisposable
    {
        public MessageTypes()
        {
            MessageTypeMap.Map<TestMessage>("test_message");
        }

        public void Dispose()
        {
            MessageTypeMap.Clear();
        }
    }
}
