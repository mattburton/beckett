#if !NET9_0_OR_GREATER
using UUIDNext;
#endif

namespace Beckett.Messages;

public static class MessageId
{
    public static Guid New()
    {
        #if NET9_0_OR_GREATER
        return Guid.CreateVersion7();
        #else
        return Uuid.NewDatabaseFriendly(UUIDNext.Database.PostgreSql);
        #endif
    }
}
