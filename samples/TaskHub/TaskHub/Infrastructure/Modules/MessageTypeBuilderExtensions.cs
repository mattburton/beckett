namespace TaskHub.Infrastructure.Modules;

public static class MessageTypeBuilderExtensions
{
    public static void MapNotification<T>(this IMessageTypeBuilder builder, string name) where T : INotification
    {
        builder.Map<T>($"notifications:{name}");
    }
}
