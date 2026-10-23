using ATOR.Models;
using Microsoft.EntityFrameworkCore;

namespace ATOR.Reports;

public sealed class PostgresTrackReportRepository(
    IDbContextFactory<TelemetryDbContext> dbContextFactory)
{
    public async Task<IReadOnlyList<string>> InitializeAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var pendingMigrations = (await dbContext.Database
            .GetPendingMigrationsAsync(cancellationToken))
            .ToArray();

        await dbContext.Database.MigrateAsync(cancellationToken);
        return pendingMigrations;
    }

    public async Task<TrackReportUpdateResult> AddIfNewerAsync(
        TrackReport report,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var affectedRows = await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO latest_tracks (
                source_id, track_id, timestamp_utc, latitude_degrees, longitude_degrees,
                altitude_meters, ground_speed_meters_per_second
            )
            VALUES (
                {report.SourceId}, {report.TrackId}, {report.TimestampUtc.UtcDateTime},
                {report.LatitudeDegrees}, {report.LongitudeDegrees}, {report.AltitudeMeters},
                {report.GroundSpeedMetersPerSecond}
            )
            ON CONFLICT (source_id, track_id) DO UPDATE SET
                timestamp_utc = EXCLUDED.timestamp_utc,
                latitude_degrees = EXCLUDED.latitude_degrees,
                longitude_degrees = EXCLUDED.longitude_degrees,
                altitude_meters = EXCLUDED.altitude_meters,
                ground_speed_meters_per_second = EXCLUDED.ground_speed_meters_per_second
            WHERE latest_tracks.timestamp_utc < EXCLUDED.timestamp_utc;
            """,
            cancellationToken);

        if (affectedRows == 0)
        {
            var currentTimestamp = await dbContext.CurrentTracks
                .Where(track => track.SourceId == report.SourceId && track.TrackId == report.TrackId)
                .Select(track => track.TimestampUtc)
                .SingleAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return report.TimestampUtc.UtcDateTime == currentTimestamp
                ? TrackReportUpdateResult.Duplicate
                : TrackReportUpdateResult.Stale;
        }

        dbContext.TelemetryReports.Add(CreateEntity(report));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return TrackReportUpdateResult.Accepted;
    }

    public async Task<CurrentTracksReport> CreateReportAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var reports = await dbContext.CurrentTracks
            .AsNoTracking()
            .OrderBy(track => track.SourceId)
            .ThenBy(track => track.TrackId)
            .Select(track => new TrackReport(
                track.SourceId,
                track.TrackId,
                new DateTimeOffset(DateTime.SpecifyKind(track.TimestampUtc, DateTimeKind.Utc)),
                track.LatitudeDegrees,
                track.LongitudeDegrees,
                track.AltitudeMeters,
                track.GroundSpeedMetersPerSecond))
            .ToListAsync(cancellationToken);

        return new CurrentTracksReport(DateTimeOffset.UtcNow, reports.Count, reports);
    }

    private static TelemetryReportEntity CreateEntity(TrackReport report) =>
        new()
        {
            SourceId = report.SourceId,
            TrackId = report.TrackId,
            TimestampUtc = report.TimestampUtc.UtcDateTime,
            LatitudeDegrees = report.LatitudeDegrees,
            LongitudeDegrees = report.LongitudeDegrees,
            AltitudeMeters = report.AltitudeMeters,
            GroundSpeedMetersPerSecond = report.GroundSpeedMetersPerSecond
        };
}
