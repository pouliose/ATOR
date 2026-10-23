using ATOR.Producer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

var port = builder.Configuration.GetValue<int>("Telemetry:UdpPort");
var intervalSeconds = builder.Configuration.GetValue<int>("Telemetry:IntervalSeconds");
var trackCount = builder.Configuration.GetValue<int>("Telemetry:TrackCount");
var sourceId = builder.Configuration["Telemetry:SourceId"]
    ?? throw new InvalidOperationException("Telemetry:SourceId must be configured.");
var receiverHost = builder.Configuration["Telemetry:ReceiverHost"]
    ?? throw new InvalidOperationException("Telemetry:ReceiverHost must be configured.");

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
ArgumentException.ThrowIfNullOrWhiteSpace(receiverHost);

builder.Services.AddSingleton(new TelemetryProducerOptions(port, intervalSeconds, trackCount, sourceId, receiverHost));
builder.Services.AddSingleton<ATOR.Simulation.TelemetrySimulator>(
    _ => new ATOR.Simulation.TelemetrySimulator(Random.Shared));
builder.Services.AddHostedService<UdpTelemetryProducerService>();

await builder.Build().RunAsync();
