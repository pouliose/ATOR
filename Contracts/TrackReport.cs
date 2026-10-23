namespace ATOR.Models;

public sealed record TrackReport(
    string SourceId,
    string TrackId,
    DateTimeOffset TimestampUtc,
    double LatitudeDegrees,
    double LongitudeDegrees,
    double AltitudeMeters,
    double GroundSpeedMetersPerSecond);
