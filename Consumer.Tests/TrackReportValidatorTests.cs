using ATOR.Models;
using ATOR.Reports;
using Xunit;

namespace ATOR.Consumer.Tests;

public sealed class TrackReportValidatorTests
{
    private static readonly DateTimeOffset ValidTimestamp =
        new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    public static TheoryData<TrackReport?, string> InvalidReports => new()
    {
        { null, "The payload must contain a telemetry report." },
        { ValidReport() with { SourceId = "" }, "SourceId is required." },
        { ValidReport() with { TrackId = " " }, "TrackId is required." },
        { ValidReport() with { TimestampUtc = default }, "TimestampUtc must be a valid UTC timestamp." },
        {
            ValidReport() with { TimestampUtc = ValidTimestamp.ToOffset(TimeSpan.FromHours(1)) },
            "TimestampUtc must be a valid UTC timestamp."
        },
        { ValidReport() with { LatitudeDegrees = -90.1 }, "LatitudeDegrees must be between -90 and 90." },
        { ValidReport() with { LatitudeDegrees = 90.1 }, "LatitudeDegrees must be between -90 and 90." },
        { ValidReport() with { LatitudeDegrees = double.NaN }, "LatitudeDegrees must be between -90 and 90." },
        { ValidReport() with { LatitudeDegrees = double.PositiveInfinity }, "LatitudeDegrees must be between -90 and 90." },
        { ValidReport() with { LongitudeDegrees = -180.1 }, "LongitudeDegrees must be between -180 and 180." },
        { ValidReport() with { LongitudeDegrees = 180.1 }, "LongitudeDegrees must be between -180 and 180." },
        { ValidReport() with { LongitudeDegrees = double.NaN }, "LongitudeDegrees must be between -180 and 180." },
        { ValidReport() with { LongitudeDegrees = double.NegativeInfinity }, "LongitudeDegrees must be between -180 and 180." },
        { ValidReport() with { AltitudeMeters = -0.1 }, "AltitudeMeters must be a finite, non-negative number." },
        { ValidReport() with { AltitudeMeters = double.PositiveInfinity }, "AltitudeMeters must be a finite, non-negative number." },
        { ValidReport() with { GroundSpeedMetersPerSecond = -0.1 }, "GroundSpeedMetersPerSecond must be a finite, non-negative number." },
        { ValidReport() with { GroundSpeedMetersPerSecond = double.NaN }, "GroundSpeedMetersPerSecond must be a finite, non-negative number." }
    };

    [Fact]
    public void TryValidate_AcceptsValidReportAndClearsError()
    {
        var isValid = TrackReportValidator.TryValidate(ValidReport(), out var error);

        Assert.True(isValid);
        Assert.Empty(error);
    }

    [Fact]
    public void TryValidate_AcceptsCoordinateBoundariesAndZeroMeasurements()
    {
        var report = ValidReport() with
        {
            LatitudeDegrees = -90,
            LongitudeDegrees = 180,
            AltitudeMeters = 0,
            GroundSpeedMetersPerSecond = 0
        };

        Assert.True(TrackReportValidator.TryValidate(report, out var error));
        Assert.Empty(error);
    }

    [Theory]
    [MemberData(nameof(InvalidReports))]
    public void TryValidate_RejectsInvalidReport(TrackReport? report, string expectedError)
    {
        var isValid = TrackReportValidator.TryValidate(report, out var error);

        Assert.False(isValid);
        Assert.Equal(expectedError, error);
    }

    private static TrackReport ValidReport() =>
        new(
            "source-1",
            "track-1",
            ValidTimestamp,
            37.9838,
            23.7275,
            1000,
            60);
}
