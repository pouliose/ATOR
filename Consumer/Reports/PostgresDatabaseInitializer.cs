using Microsoft.Extensions.Hosting;

namespace ATOR.Reports;

public sealed class PostgresDatabaseInitializer(
    PostgresTrackReportRepository repository,
    ILogger<PostgresDatabaseInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var appliedMigrations = await repository.InitializeAsync(cancellationToken);
        if (appliedMigrations.Count == 0)
        {
            logger.LogInformation("PostgreSQL schema is up to date; no pending migrations.");
            return;
        }

        logger.LogInformation(
            "Applied PostgreSQL migrations: {Migrations}",
            string.Join(", ", appliedMigrations));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
