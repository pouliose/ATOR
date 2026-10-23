using ATOR.Simulation;
using Xunit;

namespace ATOR.Producer.Tests;

public sealed class TelemetrySimulatorTests
{
    private readonly TelemetrySimulator _simulator = new();

    [Fact]
    public void GenerateReports_ReturnsRequestedNumberOfReports()
    {
        var reports = _simulator.GenerateReports(3, "source-1");

        Assert.Equal(3, reports.Count);
        Assert.All(reports, report => Assert.Equal("source-1", report.SourceId));
    }

    [Fact]
    public void GenerateReports_UsesSequentialIdsAndPredictableTrackValues()
    {
        var reports = _simulator.GenerateReports(3, "source-1");

        var expected = new[]
        {
            (TrackId: "SIM-001", Latitude: 37.9838, Longitude: 23.7275, Altitude: 1000d, Speed: 60d),
            (TrackId: "SIM-002", Latitude: 37.9938, Longitude: 23.7425, Altitude: 1150d, Speed: 62d),
            (TrackId: "SIM-003", Latitude: 38.0038, Longitude: 23.7575, Altitude: 1300d, Speed: 64d)
        };

        var actual = reports.Select(report => (
            TrackId: report.TrackId,
            Latitude: Math.Round(report.LatitudeDegrees, 4),
            Longitude: Math.Round(report.LongitudeDegrees, 4),
            Altitude: report.AltitudeMeters,
            Speed: report.GroundSpeedMetersPerSecond)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GenerateReports_UsesDescendingUtcTimestamps()
    {
        var beforeGeneration = DateTimeOffset.UtcNow;
        var reports = _simulator.GenerateReports(3, "source-1");
        var afterGeneration = DateTimeOffset.UtcNow;

        Assert.All(reports, report =>
        {
            Assert.Equal(TimeSpan.Zero, report.TimestampUtc.Offset);
            Assert.InRange(report.TimestampUtc, beforeGeneration.AddSeconds(-2), afterGeneration);
        });
        Assert.Equal(TimeSpan.FromSeconds(1), reports[0].TimestampUtc - reports[1].TimestampUtc);
        Assert.Equal(TimeSpan.FromSeconds(1), reports[1].TimestampUtc - reports[2].TimestampUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GenerateReports_RejectsNonPositiveCount(int count)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _simulator.GenerateReports(count, "source-1"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void GenerateReports_RejectsBlankSourceId(string sourceId)
    {
        Assert.Throws<ArgumentException>(
            () => _simulator.GenerateReports(1, sourceId));
    }
}
