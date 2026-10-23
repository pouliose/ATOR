using ATOR.Producer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

var port = builder.Configuration.GetValue<int?>("Telemetry:UdpPort") ?? 5000;
var intervalSeconds = builder.Configuration.GetValue<int?>("Telemetry:IntervalSeconds") ?? 1;
var trackCount = builder.Configuration.GetValue<int?>("Telemetry:TrackCount") ?? 3;
var sourceId = builder.Configuration["Telemetry:SourceId"] ?? "SIM-01";

if (port is < 1 or > 65535)
{
    throw new InvalidOperationException("Telemetry:UdpPort must be between 1 and 65535.");
}

if (intervalSeconds <= 0)
{
    throw new InvalidOperationException("Telemetry:IntervalSeconds must be greater than zero.");
}

if (trackCount <= 0)
{
    throw new InvalidOperationException("Telemetry:TrackCount must be greater than zero.");
}

ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);

builder.Services.AddSingleton(new TelemetryProducerOptions(port, intervalSeconds, trackCount, sourceId));
builder.Services.AddSingleton<ATOR.Simulation.TelemetrySimulator>();
builder.Services.AddHostedService<UdpTelemetryProducerService>();

await builder.Build().RunAsync();
