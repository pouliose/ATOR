using ATOR.Api;

namespace ATOR.Services;

public interface IAlertsService
{
    Task<IReadOnlyList<TrackAlertResponse>> GetAlertsAsync(
        int pageSize,
        CancellationToken cancellationToken);
}
