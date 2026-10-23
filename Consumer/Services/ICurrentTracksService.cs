using ATOR.Api;

namespace ATOR.Services;

public interface ICurrentTracksService
{
    Task<CurrentTracksResponse> GetCurrentAsync(CancellationToken cancellationToken);
}
