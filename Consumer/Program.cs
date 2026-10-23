using ATOR.Receiver;
using ATOR.Reports;

var builder = Host.CreateApplicationBuilder(args);

var udpPort = builder.Configuration.GetValue<int?>("Telemetry:UdpPort") ?? 5000;
if (udpPort is < 1 or > 65535)
{
    throw new InvalidOperationException("Telemetry:UdpPort must be between 1 and 65535.");
}

builder.Services.AddSingleton<TrackReportStore>();
builder.Services.AddHostedService(serviceProvider =>
    new UdpTelemetryReceiverService(
        udpPort,
        serviceProvider.GetRequiredService<TrackReportStore>(),
        serviceProvider.GetRequiredService<ILogger<UdpTelemetryReceiverService>>()));

await builder.Build().RunAsync();
