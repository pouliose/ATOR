using Microsoft.EntityFrameworkCore;

namespace ATOR.Reports;

public sealed class TelemetryDbContext(DbContextOptions<TelemetryDbContext> options)
    : DbContext(options)
{
    public DbSet<CurrentTrackEntity> CurrentTracks => Set<CurrentTrackEntity>();

    public DbSet<TelemetryReportEntity> TelemetryReports => Set<TelemetryReportEntity>();

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
