using ATOR.Models;

namespace ATOR.Alerting;

public static class TrackAlertEvaluator
{
    public static IReadOnlyList<ZoneEntryAlert> FindZoneEntries(
        TrackReport? previousReport,
        TrackReport report,
        IReadOnlyList<RestrictedZoneOptions> zones)
    {
        return zones
            .Where(zone =>
                Contains(zone, report.LatitudeDegrees, report.LongitudeDegrees) &&
                (previousReport is null ||
                    !Contains(zone, previousReport.LatitudeDegrees, previousReport.LongitudeDegrees)))
            .Select(zone => new ZoneEntryAlert(
                zone.Id,
                $"Track entered restricted zone '{zone.Id}'."))
            .ToArray();
    }

    private static bool Contains(RestrictedZoneOptions zone, double latitude, double longitude) =>
        latitude >= zone.MinimumLatitudeDegrees &&
        latitude <= zone.MaximumLatitudeDegrees &&
        longitude >= zone.MinimumLongitudeDegrees &&
        longitude <= zone.MaximumLongitudeDegrees;
}

public sealed record ZoneEntryAlert(string ZoneId, string Details);
