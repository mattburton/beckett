using Beckett.Messages;

namespace Beckett.Tests;

public record TestMessage(Guid Id);

public record AnotherTestMessage(Guid Id);

public record TestEvent(int Number);

public static class MessageRegistry
{
    public static void Register()
    {
        MessageTypeMap.Map<TestMessage>("test-message");
        MessageTypeMap.Map<AnotherTestMessage>("another-test-message");
        MessageTypeMap.Map<TestEvent>("test-event");
    }
}
