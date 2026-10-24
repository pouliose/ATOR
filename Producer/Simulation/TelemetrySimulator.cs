using ATOR.Models;

namespace ATOR.Simulation;

public sealed class TelemetrySimulator
{
    private const double CoordinateJitterDegrees = 0.0001;
    private readonly Random _random;

    public TelemetrySimulator() : this(Random.Shared)
    {
    }

    public TelemetrySimulator(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        _random = random;
    }

    public IReadOnlyList<TrackReport> GenerateReports(
        int count,
        string sourceId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);

        var generatedAt = DateTimeOffset.UtcNow;
        var reports = new List<TrackReport>(count);

        for (var index = 0; index < count; index++)
        {
            var baseLatitude = 37.9838 + (index * 0.01);
            var baseLongitude = 23.7275 + (index * 0.015);
            reports.Add(new TrackReport(
                SourceId: sourceId,
                TrackId: $"DRONE-{index + 1:000}",
                TimestampUtc: generatedAt.AddSeconds(-index),
                LatitudeDegrees: baseLatitude + GetCoordinateJitter(),
                LongitudeDegrees: baseLongitude + GetCoordinateJitter(),
                AltitudeMeters: 1000 + (index * 150),
                GroundSpeedMetersPerSecond: 60 + (index * 2)));
        }

        return reports;
    }

    private double GetCoordinateJitter() =>
        (_random.NextDouble() * 2 - 1) * CoordinateJitterDegrees;
}
