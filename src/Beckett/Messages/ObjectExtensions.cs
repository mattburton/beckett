namespace Beckett.Messages;

public static class ObjectExtensions
{
    public static string GetMessageType(this object message)
    {
        if (message is Message genericMessage)
        {
            return genericMessage.Type;
        }

        var actualMessage = message;

        if (actualMessage is MessageMetadataWrapper messageMetadataWrapper)
        {
            actualMessage = messageMetadataWrapper.Message;
        }

        if (actualMessage is Message nestedGenericMessage)
        {
            return nestedGenericMessage.Type;
        }

        return MessageTypeMap.GetName(actualMessage.GetType());
    }
}
