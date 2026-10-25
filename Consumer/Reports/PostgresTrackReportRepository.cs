using ATOR.Models;
using ATOR.Alerting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATOR.Reports;

public sealed class PostgresTrackReportRepository(
    IDbContextFactory<TelemetryDbContext> dbContextFactory,
    IOptions<AlertingOptions> alertingOptions) : ICurrentTracksReader, IAlertsReader
{
    private readonly AlertingOptions _alertingOptions = alertingOptions.Value;

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
        var previousTrack = await dbContext.CurrentTracks
            .AsNoTracking()
            .SingleOrDefaultAsync(
                track => track.SourceId == report.SourceId && track.TrackId == report.TrackId,
                cancellationToken);

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
        var previousReport = previousTrack is null
            ? null
            : new TrackReport(
                previousTrack.SourceId,
                previousTrack.TrackId,
                new DateTimeOffset(DateTime.SpecifyKind(previousTrack.TimestampUtc, DateTimeKind.Utc)),
                previousTrack.LatitudeDegrees,
                previousTrack.LongitudeDegrees,
                previousTrack.AltitudeMeters,
                previousTrack.GroundSpeedMetersPerSecond);
        foreach (var zoneEntry in TrackAlertEvaluator.FindZoneEntries(
                     previousReport,
                     report,
                     _alertingOptions.Zones))
        {
            dbContext.Alerts.Add(new TrackAlertEntity
            {
                Type = AlertTypes.ZoneEntry,
                SourceId = report.SourceId,
                TrackId = report.TrackId,
                OccurredAtUtc = report.TimestampUtc.UtcDateTime,
                ObservedTimestampUtc = report.TimestampUtc.UtcDateTime,
                ZoneId = zoneEntry.ZoneId,
                DeduplicationKey = zoneEntry.ZoneId,
                Details = zoneEntry.Details
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return TrackReportUpdateResult.Accepted;
    }

    public async Task<int> AddTransmissionLostAlertsAsync(
        DateTimeOffset checkedAtUtc,
        int staleTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var cutoffUtc = checkedAtUtc.UtcDateTime.AddSeconds(-staleTimeoutSeconds);
        var details = $"No telemetry received for {staleTimeoutSeconds} seconds.";

        return await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO track_alerts (
                type, source_id, track_id, occurred_at_utc, observed_timestamp_utc,
                zone_id, details, deduplication_key
            )
            SELECT
                {AlertTypes.TransmissionLost},
                source_id,
                track_id,
                -- Record the stale threshold crossing, not when this periodic query noticed it.
                timestamp_utc + make_interval(secs => {staleTimeoutSeconds}),
                timestamp_utc,
                NULL,
                {details},
                {AlertTypes.TransmissionLost}
            FROM latest_tracks
            WHERE timestamp_utc <= {cutoffUtc}
            ON CONFLICT (
                type, source_id, track_id, observed_timestamp_utc, deduplication_key
            ) DO NOTHING;
            """,
            cancellationToken);
    }

    public async Task<IReadOnlyList<TrackAlert>> GetAlertsAsync(
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var alerts = await dbContext.Alerts
            .AsNoTracking()
            .OrderByDescending(alert => alert.OccurredAtUtc)
            .ThenByDescending(alert => alert.Id)
            .Take(pageSize)
            .Select(alert => new TrackAlert(
                alert.Id,
                alert.Type,
                alert.SourceId,
                alert.TrackId,
                new DateTimeOffset(DateTime.SpecifyKind(alert.OccurredAtUtc, DateTimeKind.Utc)),
                new DateTimeOffset(DateTime.SpecifyKind(alert.ObservedTimestampUtc, DateTimeKind.Utc)),
                alert.ZoneId,
                alert.Details))
            .ToListAsync(cancellationToken);

        return alerts;
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

    public async Task<TrackHistoryPage> GetHistoryAsync(
        string trackId,
        int pageSize,
        DateTimeOffset? beforeTimestampUtc,
        string? beforeSourceId,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = dbContext.TelemetryReports
            .AsNoTracking()
            .Where(report => report.TrackId == trackId);

        if (beforeTimestampUtc is not null && beforeSourceId is not null)
        {
            var cursorTimestampUtc = beforeTimestampUtc.Value.UtcDateTime;
            query = query.Where(report =>
                report.TimestampUtc < cursorTimestampUtc ||
                (report.TimestampUtc == cursorTimestampUtc &&
                    string.Compare(report.SourceId, beforeSourceId) < 0));
        }

        var page = await query
            .OrderByDescending(report => report.TimestampUtc)
            .ThenByDescending(report => report.SourceId)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = page.Count > pageSize;
        var reports = page
            .Take(pageSize)
            .Select(report => new TrackReport(
                report.SourceId,
                report.TrackId,
                new DateTimeOffset(DateTime.SpecifyKind(report.TimestampUtc, DateTimeKind.Utc)),
                report.LatitudeDegrees,
                report.LongitudeDegrees,
                report.AltitudeMeters,
                report.GroundSpeedMetersPerSecond))
            .ToArray();

        return new TrackHistoryPage(reports, hasMore);
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
