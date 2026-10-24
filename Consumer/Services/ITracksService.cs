using ATOR.Api;

namespace ATOR.Services;

public interface ITracksService
{
    Task<CurrentTracksResponse> GetCurrentAsync(CancellationToken cancellationToken);

    Task<TrackHistoryResponse> GetHistoryAsync(
        string trackId,
        int pageSize,
        DateTimeOffset? beforeTimestampUtc,
        string? beforeSourceId,
        CancellationToken cancellationToken);
}
