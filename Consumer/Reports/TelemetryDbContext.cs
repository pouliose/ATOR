using Microsoft.EntityFrameworkCore;

namespace ATOR.Reports;

public sealed class TelemetryDbContext(DbContextOptions<TelemetryDbContext> options)
    : DbContext(options)
{
    public DbSet<CurrentTrackEntity> CurrentTracks => Set<CurrentTrackEntity>();

    public DbSet<TelemetryReportEntity> TelemetryReports => Set<TelemetryReportEntity>();

    public DbSet<TrackAlertEntity> Alerts => Set<TrackAlertEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CurrentTrackEntity>(entity =>
        {
            entity.ToTable("latest_tracks");
            entity.HasKey(track => new { track.SourceId, track.TrackId });
            entity.Property(track => track.SourceId).HasColumnName("source_id");
            entity.Property(track => track.TrackId).HasColumnName("track_id");
            entity.Property(track => track.TimestampUtc).HasColumnName("timestamp_utc");
            entity.Property(track => track.LatitudeDegrees).HasColumnName("latitude_degrees");
            entity.Property(track => track.LongitudeDegrees).HasColumnName("longitude_degrees");
            entity.Property(track => track.AltitudeMeters).HasColumnName("altitude_meters");
            entity.Property(track => track.GroundSpeedMetersPerSecond)
                .HasColumnName("ground_speed_meters_per_second");
        });

        modelBuilder.Entity<TelemetryReportEntity>(entity =>
        {
            entity.ToTable("telemetry_reports");
            entity.HasKey(report => new { report.SourceId, report.TrackId, report.TimestampUtc });
            entity.Property(report => report.SourceId).HasColumnName("source_id");
            entity.Property(report => report.TrackId).HasColumnName("track_id");
            entity.Property(report => report.TimestampUtc).HasColumnName("timestamp_utc");
            entity.Property(report => report.LatitudeDegrees).HasColumnName("latitude_degrees");
            entity.Property(report => report.LongitudeDegrees).HasColumnName("longitude_degrees");
            entity.Property(report => report.AltitudeMeters).HasColumnName("altitude_meters");
            entity.Property(report => report.GroundSpeedMetersPerSecond)
                .HasColumnName("ground_speed_meters_per_second");
            entity.HasIndex(report => new
                {
                    report.TrackId,
                    report.TimestampUtc,
                    report.SourceId
                })
                .HasDatabaseName("IX_telemetry_reports_track_id_timestamp_utc_source_id");
        });

        modelBuilder.Entity<TrackAlertEntity>(entity =>
        {
            entity.ToTable("track_alerts");
            entity.HasKey(alert => alert.Id);
            entity.Property(alert => alert.Id).HasColumnName("id").UseIdentityByDefaultColumn();
            entity.Property(alert => alert.Type).HasColumnName("type");
            entity.Property(alert => alert.SourceId).HasColumnName("source_id");
            entity.Property(alert => alert.TrackId).HasColumnName("track_id");
            entity.Property(alert => alert.OccurredAtUtc).HasColumnName("occurred_at_utc");
            entity.Property(alert => alert.ObservedTimestampUtc).HasColumnName("observed_timestamp_utc");
            entity.Property(alert => alert.DeduplicationKey).HasColumnName("deduplication_key");
            entity.Property(alert => alert.ZoneId).HasColumnName("zone_id");
            entity.Property(alert => alert.Details).HasColumnName("details");
            entity.HasIndex(alert => new
                {
                    alert.Type,
                    alert.SourceId,
                    alert.TrackId,
                    alert.ObservedTimestampUtc,
                    alert.DeduplicationKey
                })
                .IsUnique()
                .HasDatabaseName("IX_track_alerts_type_source_track_observed_timestamp");
            entity.HasIndex(alert => new { alert.OccurredAtUtc, alert.Id })
                .HasDatabaseName("IX_track_alerts_occurred_at_utc_id");
        });
    }
}

public sealed class TelemetryReportEntity
{
    public string SourceId { get; set; } = string.Empty;

    public string TrackId { get; set; } = string.Empty;

    public DateTime TimestampUtc { get; set; }

    public double LatitudeDegrees { get; set; }

    public double LongitudeDegrees { get; set; }

    public double AltitudeMeters { get; set; }

    public double GroundSpeedMetersPerSecond { get; set; }
}

public sealed class CurrentTrackEntity
{
    public string SourceId { get; set; } = string.Empty;

    public string TrackId { get; set; } = string.Empty;

    public DateTime TimestampUtc { get; set; }

    public double LatitudeDegrees { get; set; }

    public double LongitudeDegrees { get; set; }

    public double AltitudeMeters { get; set; }

    public double GroundSpeedMetersPerSecond { get; set; }
}

public sealed class TrackAlertEntity
{
    public long Id { get; set; }

    public string Type { get; set; } = string.Empty;

    public string SourceId { get; set; } = string.Empty;

    public string TrackId { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }

    public DateTime ObservedTimestampUtc { get; set; }

    public string DeduplicationKey { get; set; } = string.Empty;

    public string? ZoneId { get; set; }

    public string Details { get; set; } = string.Empty;
}
