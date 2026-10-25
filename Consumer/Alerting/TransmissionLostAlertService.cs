using ATOR.Reports;
using Microsoft.Extensions.Options;

namespace ATOR.Alerting;

public sealed class TransmissionLostAlertService(
    PostgresTrackReportRepository repository,
    IOptions<AlertingOptions> alertingOptions,
    ILogger<TransmissionLostAlertService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = alertingOptions.Value;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.CheckIntervalSeconds));

        do
        {
            var createdAlerts = await repository.AddTransmissionLostAlertsAsync(
                DateTimeOffset.UtcNow,
                options.StaleTimeoutSeconds,
                stoppingToken);
            if (createdAlerts > 0)
            {
                logger.LogInformation(
                    "Created {AlertCount} transmission-lost alert(s).",
                    createdAlerts);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
