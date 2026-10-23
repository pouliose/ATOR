using ATOR.Models;

namespace ATOR.Reports;

public sealed class TrackReportStore
{
    private readonly Dictionary<(string SourceId, string TrackId), TrackReport> _latestReports = [];

    public TrackReportUpdateResult AddOrUpdate(TrackReport report)
    {
        var key = (report.SourceId, report.TrackId);
        if (!_latestReports.TryGetValue(key, out var current))
        {
            _latestReports.Add(key, report);
            return TrackReportUpdateResult.Accepted;
        }

        if (report.TimestampUtc == current.TimestampUtc)
        {
            return TrackReportUpdateResult.Duplicate;
        }

        if (report.TimestampUtc < current.TimestampUtc)
        {
            return TrackReportUpdateResult.Stale;
        }

        _latestReports[key] = report;
        return TrackReportUpdateResult.Accepted;
    }

    public CurrentTracksReport CreateReport()
    {
        var tracks = _latestReports.Values
            .OrderBy(report => report.SourceId, StringComparer.Ordinal)
            .ThenBy(report => report.TrackId, StringComparer.Ordinal)
            .ToArray();

        return new CurrentTracksReport(DateTimeOffset.UtcNow, tracks.Length, tracks);
    }
}

public sealed record CurrentTracksReport(
    DateTimeOffset GeneratedAtUtc,
    int TrackCount,
    IReadOnlyList<TrackReport> Tracks);

public enum TrackReportUpdateResult
{
    Accepted,
    Duplicate,
    Stale
}
