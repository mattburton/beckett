using System.Reflection;
using Beckett;
using Beckett.Messages;
using Core.ReadModels;

namespace Core.Tests.SourceGenerators;

public class ReadModelGeneratorTests
{
    public class when_class_is_not_nested
    {
        [Fact]
        public void apply_method_is_generated()
        {
            var method = typeof(TestReadModel).GetMethod(
                nameof(TestReadModel.Apply),
                BindingFlags.Public | BindingFlags.Instance,
                [typeof(IMessageContext)]
            );

            Assert.NotNull(method);
        }

        [Fact]
        public void applied_message_types_are_available()
        {
            var state = new TestReadModel();

            Assert.Collection(
                state.AppliedMessageTypes(),
                type => Assert.Equal(typeof(TestMessage), type),
                type => Assert.Equal(typeof(TestMessageWithContext), type)
            );
        }

        [Fact]
        public void applies_message()
        {
            var state = new TestReadModel();
            const int expectedValue = 1;
            var message = new TestMessage(expectedValue);
            var context = MessageContext.From(message);

            state.Apply(context);

            Assert.Equal(expectedValue, state.Value);
        }

        [Fact]
        public void applies_message_with_context()
        {
            var state = new TestReadModel();
            const int expectedValue = 1;
            var message = new TestMessageWithContext(expectedValue);
            var context = MessageContext.From(message);

            state.Apply(context);

            Assert.Equal(expectedValue, state.Value);
            Assert.True(state.ContextSupplied);
        }
    }

    public class when_class_is_nested
    {
        [Fact]
        public void apply_method_is_generated_for_nested_class()
        {
            var method = typeof(TestContainingClass.NestedReadModel).GetMethod(
                nameof(TestReadModel.Apply),
                BindingFlags.Public | BindingFlags.Instance,
                [typeof(IMessageContext)]
            );

            Assert.NotNull(method);
        }
    }

    public class when_class_is_nested_inside_record
    {
        [Fact]
        public void apply_method_is_generated_for_nested_class()
        {
            var method = typeof(TestContainingRecord.NestedReadModel).GetMethod(
                nameof(TestReadModel.Apply),
                BindingFlags.Public | BindingFlags.Instance,
                [typeof(IMessageContext)]
            );

            Assert.NotNull(method);
        }
    }
}

[ReadModel]
public partial class TestReadModel
{
    public int Value { get; private set; }
    public bool ContextSupplied { get; private set; }

    private void Apply(TestMessage m) => Value = m.Value;

    private void Apply(TestMessageWithContext m, IMessageContext _)
    {
        Value = m.Value;
        ContextSupplied = true;
    }
}

public partial class TestContainingClass
{
    [ReadModel]
    public partial class NestedReadModel
    {
        private void Apply(TestMessage _)
        {
        }
    }
}

public partial record TestContainingRecord
{
    [ReadModel]
    public partial class NestedReadModel
    {
        private void Apply(TestMessage _)
        {
        }
    }
}

public record TestMessage(int Value);

public record TestMessageWithContext(int Value);
