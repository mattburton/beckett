using OpenTelemetry.Trace;

namespace Beckett.OpenTelemetry;

public static class TracerProviderBuilderExtensions
{
    public static TracerProviderBuilder AddBeckett(this TracerProviderBuilder builder) =>
        builder.AddSource(TelemetryConstants.ActivitySource.Name);
}
