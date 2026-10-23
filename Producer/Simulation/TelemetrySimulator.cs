using ATOR.Models;

namespace ATOR.Simulation;

public sealed class TelemetrySimulator
{
    public IReadOnlyList<TrackReport> GenerateReports(
        int count = 3,
        string sourceId = "SIM-01")
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);

        var generatedAt = DateTimeOffset.UtcNow;
        var reports = new List<TrackReport>(count);

        for (var index = 0; index < count; index++)
        {
            reports.Add(new TrackReport(
                SourceId: sourceId,
                TrackId: $"SIM-{index + 1:000}",
                TimestampUtc: generatedAt.AddSeconds(-index),
                LatitudeDegrees: 37.9838 + (index * 0.01),
                LongitudeDegrees: 23.7275 + (index * 0.015),
                AltitudeMeters: 1000 + (index * 150),
                GroundSpeedMetersPerSecond: 60 + (index * 2)));
        }

        return reports;
    }
}
