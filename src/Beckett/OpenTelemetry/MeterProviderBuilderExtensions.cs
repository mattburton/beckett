using OpenTelemetry.Metrics;

namespace Beckett.OpenTelemetry;

public static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddBeckett(this MeterProviderBuilder builder)
    {
        builder.AddMeter(TelemetryConstants.ActivitySource.Name);

        return builder;
    }
}
