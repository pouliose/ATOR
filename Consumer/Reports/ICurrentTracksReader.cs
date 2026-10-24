using ATOR.Models;

namespace ATOR.Reports;

public interface ICurrentTracksReader
{
    Task<CurrentTracksReport> CreateReportAsync(CancellationToken cancellationToken);

    Task<TrackHistoryPage> GetHistoryAsync(
        string trackId,
        int pageSize,
        DateTimeOffset? beforeTimestampUtc,
        string? beforeSourceId,
        CancellationToken cancellationToken);
}

public sealed record TrackHistoryPage(
    IReadOnlyList<TrackReport> Reports,
    bool HasMore);
