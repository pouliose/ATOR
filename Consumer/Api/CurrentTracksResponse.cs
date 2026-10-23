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
