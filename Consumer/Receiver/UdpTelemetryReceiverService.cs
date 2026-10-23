using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ATOR.Models;
using ATOR.Reports;

namespace ATOR.Receiver;

public sealed class UdpTelemetryReceiverService(
    int port,
    TrackReportStore reportStore,
    ILogger<UdpTelemetryReceiverService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var receiver = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
        logger.LogInformation(
            "Telemetry consumer listening on 127.0.0.1:{Port}. Press Ctrl+C to stop.",
            port);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var datagram = await receiver.ReceiveAsync(stoppingToken);
                TrackReport? report;
                try
                {
                    report = JsonSerializer.Deserialize<TrackReport>(datagram.Buffer, JsonOptions);
                }
                catch (JsonException exception)
                {
                    logger.LogWarning(
                        "Rejected malformed JSON telemetry from {RemoteEndpoint}: {Error}",
                        datagram.RemoteEndPoint,
                        exception.Message);
                    continue;
                }

                if (!TrackReportValidator.TryValidate(report, out var validationError))
                {
                    logger.LogWarning(
                        "Rejected invalid telemetry from {RemoteEndpoint}: {Error}",
                        datagram.RemoteEndPoint,
                        validationError);
                    continue;
                }

                var updateResult = reportStore.AddOrUpdate(report!);
                if (updateResult != TrackReportUpdateResult.Accepted)
                {
                    logger.LogInformation(
                        "Ignored {UpdateResult} telemetry for {SourceId}/{TrackId}.",
                        updateResult,
                        report!.SourceId,
                        report.TrackId);
                    continue;
                }

                var reportJson = JsonSerializer.Serialize(reportStore.CreateReport(), JsonOptions);
                logger.LogInformation("Current tracks report:\n{ReportJson}", reportJson);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger.LogInformation("Telemetry consumer stopped.");
    }
}
