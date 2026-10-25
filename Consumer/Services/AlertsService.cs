using ATOR.Alerting;
using ATOR.Api;

namespace ATOR.Services;

public sealed class AlertsService(IAlertsReader alertsReader) : IAlertsService
{
    public async Task<IReadOnlyList<TrackAlertResponse>> GetAlertsAsync(
        int pageSize,
        CancellationToken cancellationToken)
    {
        var alerts = await alertsReader.GetAlertsAsync(pageSize, cancellationToken);
        return alerts
            .Select(alert => new TrackAlertResponse(
                alert.Id,
                alert.Type,
                alert.SourceId,
                alert.TrackId,
                alert.OccurredAtUtc,
                alert.ObservedTimestampUtc,
                alert.ZoneId,
                alert.Details))
            .ToArray();
    }
}
