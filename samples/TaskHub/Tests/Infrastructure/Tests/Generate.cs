namespace Tests.Infrastructure.Tests;

public class Generate
{
    public static readonly Generate Extensions = new();

    public static Guid Guid() => System.Guid.NewGuid();

    public static string String() => System.Guid.NewGuid().ToString();
}
