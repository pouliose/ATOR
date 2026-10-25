namespace ATOR.Api;

/// <summary>Represents a persisted alert associated with a simulated track.</summary>
/// <param name="OccurredAtUtc">
/// When the alert condition began. For transmission loss, this is the last report timestamp plus the configured stale timeout,
/// not the later time when the periodic checker detected it.
/// </param>
/// <param name="ObservedTimestampUtc">
/// The timestamp of the telemetry report on which the alert is based. For transmission loss, this is the last accepted report.
/// </param>
public sealed record TrackAlertResponse(
    long Id,
    string Type,
    string SourceId,
    string TrackId,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset ObservedTimestampUtc,
    string? ZoneId,
    string Details);
