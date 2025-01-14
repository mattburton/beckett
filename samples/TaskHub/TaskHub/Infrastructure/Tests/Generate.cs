namespace TaskHub.Infrastructure.Tests;

public class Generate
{
    public static Guid Guid() => System.Guid.NewGuid();

    public static string String() => System.Guid.NewGuid().ToString();
}
