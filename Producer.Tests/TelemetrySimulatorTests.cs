using ATOR.Simulation;
using Xunit;

namespace ATOR.Producer.Tests;

public sealed class TelemetrySimulatorTests
{
    [Fact]
    public void GenerateReports_ReturnsRequestedNumberOfReports()
    {
        var simulator = CreateSimulator();
        var reports = simulator.GenerateReports(3, "source-1");

        Assert.Equal(3, reports.Count);
        Assert.All(reports, report => Assert.Equal("source-1", report.SourceId));
    }

    [Fact]
    public void GenerateReports_UsesSequentialIdsAndPredictableTrackValues()
    {
        var simulator = CreateSimulator();
        var reports = simulator.GenerateReports(3, "source-1");

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
        var simulator = CreateSimulator();
        var beforeGeneration = DateTimeOffset.UtcNow;
        var reports = simulator.GenerateReports(3, "source-1");
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
        var simulator = CreateSimulator();
        Assert.Throws<ArgumentOutOfRangeException>(
            () => simulator.GenerateReports(count, "source-1"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void GenerateReports_RejectsBlankSourceId(string sourceId)
    {
        var simulator = CreateSimulator();
        Assert.Throws<ArgumentException>(
            () => simulator.GenerateReports(1, sourceId));
    }

    [Fact]
    public void GenerateReports_AddsOnlySmallCoordinateVariations()
    {
        var simulator = new TelemetrySimulator(new Random(12345));

        var reports = simulator.GenerateReports(10, "source-1");

        for (var index = 0; index < reports.Count; index++)
        {
            var report = reports[index];
            var baseLatitude = 37.9838 + (index * 0.01);
            var baseLongitude = 23.7275 + (index * 0.015);

            Assert.InRange(Math.Abs(report.LatitudeDegrees - baseLatitude), 0, 0.0001000001);
            Assert.InRange(Math.Abs(report.LongitudeDegrees - baseLongitude), 0, 0.0001000001);
        }

        Assert.Contains(reports.Zip(Enumerable.Range(0, reports.Count)), pair =>
            pair.First.LatitudeDegrees != 37.9838 + (pair.Second * 0.01) ||
            pair.First.LongitudeDegrees != 23.7275 + (pair.Second * 0.015));
    }

    [Fact]
    public void GenerateReports_ProducesReproducibleCoordinatesWithSameRandomSeed()
    {
        var firstSimulator = new TelemetrySimulator(new Random(42));
        var secondSimulator = new TelemetrySimulator(new Random(42));

        var firstReports = firstSimulator.GenerateReports(3, "source-1");
        var secondReports = secondSimulator.GenerateReports(3, "source-1");

        Assert.Equal(
            firstReports.Select(report => (report.LatitudeDegrees, report.LongitudeDegrees)),
            secondReports.Select(report => (report.LatitudeDegrees, report.LongitudeDegrees)));
    }

    private static TelemetrySimulator CreateSimulator() =>
        new(new FixedRandom(0.5));

    private sealed class FixedRandom(double value) : Random
    {
        public override double NextDouble() => value;
    }
}
