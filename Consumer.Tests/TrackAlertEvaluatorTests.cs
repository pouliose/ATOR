using ATOR.Alerting;
using ATOR.Models;
using Xunit;

namespace ATOR.Consumer.Tests;

public sealed class TrackAlertEvaluatorTests
{
    private static readonly RestrictedZoneOptions Zone = new()
    {
        Id = "zone-1",
        MinimumLatitudeDegrees = 10,
        MaximumLatitudeDegrees = 20,
        MinimumLongitudeDegrees = 30,
        MaximumLongitudeDegrees = 40
    };

    [Fact]
    public void FindZoneEntries_ReportsEntryFromOutsideAndIncludesBoundaries()
    {
        var previous = CreateReport(9.9, 30);
        var current = CreateReport(10, 30);

        var alert = Assert.Single(TrackAlertEvaluator.FindZoneEntries(previous, current, [Zone]));

        Assert.Equal("zone-1", alert.ZoneId);
        Assert.Contains("zone-1", alert.Details);
    }

    [Fact]
    public void FindZoneEntries_TreatsFirstReportInsideAsEntry()
    {
        var alerts = TrackAlertEvaluator.FindZoneEntries(null, CreateReport(15, 35), [Zone]);

        Assert.Single(alerts);
    }

    [Fact]
    public void FindZoneEntries_DoesNotReportWhileTrackRemainsInside()
    {
        var alerts = TrackAlertEvaluator.FindZoneEntries(
            CreateReport(15, 35),
            CreateReport(16, 36),
            [Zone]);

        Assert.Empty(alerts);
    }

    [Fact]
    public void FindZoneEntries_ReportsReentryAfterTrackLeaves()
    {
        var alerts = TrackAlertEvaluator.FindZoneEntries(
            CreateReport(20.1, 35),
            CreateReport(20, 35),
            [Zone]);

        Assert.Single(alerts);
    }

    [Fact]
    public void OptionsValidator_RejectsInvalidTimeoutAndZoneBounds()
    {
        var invalidTimeoutError = AlertingOptionsValidator.GetValidationError(new AlertingOptions
        {
            StaleTimeoutSeconds = 0
        });
        var invalidZoneError = AlertingOptionsValidator.GetValidationError(new AlertingOptions
        {
            Zones =
            [
                new RestrictedZoneOptions
                {
                    Id = Zone.Id,
                    MinimumLatitudeDegrees = 21,
                    MaximumLatitudeDegrees = Zone.MaximumLatitudeDegrees,
                    MinimumLongitudeDegrees = Zone.MinimumLongitudeDegrees,
                    MaximumLongitudeDegrees = Zone.MaximumLongitudeDegrees
                }
            ]
        });

        Assert.NotNull(invalidTimeoutError);
        Assert.Contains("StaleTimeoutSeconds", invalidTimeoutError);
        Assert.NotNull(invalidZoneError);
        Assert.Contains("valid minimum and maximum latitudes", invalidZoneError);
    }

    private static TrackReport CreateReport(double latitude, double longitude) =>
        new(
            "source-1",
            "track-1",
            new DateTimeOffset(2026, 10, 25, 12, 0, 0, TimeSpan.Zero),
            latitude,
            longitude,
            100,
            20);
}
