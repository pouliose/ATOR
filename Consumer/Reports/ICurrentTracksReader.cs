namespace ATOR.Reports;

public interface ICurrentTracksReader
{
    Task<CurrentTracksReport> CreateReportAsync(CancellationToken cancellationToken);
}
