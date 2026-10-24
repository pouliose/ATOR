using ATOR.Api;
using ATOR.Reports;

namespace ATOR.Services;

public sealed class TracksService(ICurrentTracksReader tracksReader) : ITracksService
{
    public async Task<CurrentTracksResponse> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var report = await tracksReader.CreateReportAsync(cancellationToken);
        var tracks = report.Tracks
            .Select(track => new TrackPositionResponse(
                track.SourceId,
                track.TrackId,
                track.TimestampUtc,
                track.LatitudeDegrees,
                track.LongitudeDegrees,
                track.AltitudeMeters,
                track.GroundSpeedMetersPerSecond))
            .ToArray();

        return new CurrentTracksResponse(report.GeneratedAtUtc, report.TrackCount, tracks);
    }

    public async Task<TrackHistoryResponse> GetHistoryAsync(
        string trackId,
        int pageSize,
        DateTimeOffset? beforeTimestampUtc,
        string? beforeSourceId,
        CancellationToken cancellationToken)
    {
        var history = await tracksReader.GetHistoryAsync(
            trackId,
            pageSize,
            beforeTimestampUtc,
            beforeSourceId,
            cancellationToken);
        var reports = history.Reports
            .Select(track => new TrackPositionResponse(
                track.SourceId,
                track.TrackId,
                track.TimestampUtc,
                track.LatitudeDegrees,
                track.LongitudeDegrees,
                track.AltitudeMeters,
                track.GroundSpeedMetersPerSecond))
            .ToArray();
        var lastReport = reports.LastOrDefault();
        var nextCursor = history.HasMore && lastReport is not null
            ? new TrackHistoryCursor(lastReport.TimestampUtc, lastReport.SourceId)
            : null;

        return new TrackHistoryResponse(trackId, reports.Length, history.HasMore, nextCursor, reports);
    }
}
