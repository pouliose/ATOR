using ATOR.Api;
using ATOR.Reports;

namespace ATOR.Services;

public sealed class CurrentTracksService(ICurrentTracksReader tracksReader) : ICurrentTracksService
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
}
