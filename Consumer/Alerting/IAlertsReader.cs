namespace ATOR.Alerting;

public interface IAlertsReader
{
    Task<IReadOnlyList<TrackAlert>> GetAlertsAsync(
        int pageSize,
        CancellationToken cancellationToken);
}

public sealed record TrackAlert(
    long Id,
    string Type,
    string SourceId,
    string TrackId,
    // OccurredAtUtc is the condition time; for transmission loss it is the last report time plus the stale timeout.
    DateTimeOffset OccurredAtUtc,
    // ObservedTimestampUtc is the report time that triggered or was used to evaluate the alert.
    DateTimeOffset ObservedTimestampUtc,
    string? ZoneId,
    string Details);
