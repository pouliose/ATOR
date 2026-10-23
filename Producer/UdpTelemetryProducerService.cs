using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using ATOR.Simulation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ATOR.Producer;

public sealed record TelemetryProducerOptions(
    int Port,
    int IntervalSeconds,
    int TrackCount,
    string SourceId,
    string ReceiverHost);

public sealed class UdpTelemetryProducerService(
    TelemetryProducerOptions options,
    TelemetrySimulator simulator,
    ILogger<UdpTelemetryProducerService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var sender = new UdpClient(AddressFamily.InterNetwork);
        var receiverAddress = (await Dns.GetHostAddressesAsync(options.ReceiverHost))
            .FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork)
            ?? throw new InvalidOperationException(
                $"Receiver host '{options.ReceiverHost}' did not resolve to an IPv4 address.");
        var receiver = new IPEndPoint(receiverAddress, options.Port);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.IntervalSeconds));

        logger.LogInformation(
            "Telemetry producer {SourceId} sending {TrackCount} tracks to {ReceiverHost}:{Port} every {IntervalSeconds} seconds.",
            options.SourceId,
            options.TrackCount,
            options.ReceiverHost,
            options.Port,
            options.IntervalSeconds);

        try
        {
            do
            {
                var reports = simulator.GenerateReports(options.TrackCount, options.SourceId);
                foreach (var report in reports)
                {
                    var payload = JsonSerializer.SerializeToUtf8Bytes(report, JsonOptions);
                    await sender.SendAsync(payload, receiver, stoppingToken);
                    logger.LogInformation(
                        "Sent telemetry for {SourceId}/{TrackId}.",
                        report.SourceId,
                        report.TrackId);
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Telemetry producer stopped.");
        }
    }
}
