namespace ATOR.Api;

/// <summary>Represents the latest known positions of all tracked objects.</summary>
public sealed record CurrentTracksResponse(
    DateTimeOffset GeneratedAtUtc,
    int TrackCount,
    IReadOnlyList<TrackPositionResponse> Tracks);

/// <summary>Represents the latest known position and motion of a tracked object.</summary>
public sealed record TrackPositionResponse(
    string SourceId,
    string TrackId,
    DateTimeOffset TimestampUtc,
    double LatitudeDegrees,
    double LongitudeDegrees,
    double AltitudeMeters,
    double GroundSpeedMetersPerSecond);

/// <summary>Represents one page of telemetry history for a track ID.</summary>
public sealed record TrackHistoryResponse(
    string TrackId,
    int ReportCount,
    bool HasMore,
    TrackHistoryCursor? NextCursor,
    IReadOnlyList<TrackPositionResponse> Reports);

/// <summary>Identifies the last report in a history page for fetching the next page.</summary>
public sealed record TrackHistoryCursor(
    DateTimeOffset BeforeTimestampUtc,
    string BeforeSourceId);
