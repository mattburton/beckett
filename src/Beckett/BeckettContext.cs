using System.Diagnostics;
using Beckett.OpenTelemetry;

namespace Beckett;

public static class BeckettContext
{
    private const string DefaultTenant = "default";

    public static string? GetCorrelationId() =>
        Activity.Current?.GetBaggageItem(TelemetryConstants.Message.CorrelationId);

    public static string GetTenant() =>
        Activity.Current?.GetBaggageItem(TelemetryConstants.Message.Tenant) ?? DefaultTenant;

    public static void SetCorrelationId(string correlationId)
    {
        Activity.Current?.AddBaggage(TelemetryConstants.Message.CorrelationId, correlationId);
    }

    public static void SetTenant(string? tenant)
    {
        Activity.Current?.AddBaggage(
            TelemetryConstants.Message.Tenant,
            string.IsNullOrEmpty(tenant) ? DefaultTenant : tenant
        );
    }
}
