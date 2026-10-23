using ATOR.Models;

namespace ATOR.Reports;

public static class TrackReportValidator
{
    public static bool TryValidate(TrackReport? report, out string error)
    {
        if (report is null)
        {
            error = "The payload must contain a telemetry report.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(report.SourceId))
        {
            error = "SourceId is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(report.TrackId))
        {
            error = "TrackId is required.";
            return false;
        }

        if (report.TimestampUtc == default || report.TimestampUtc.Offset != TimeSpan.Zero)
        {
            error = "TimestampUtc must be a valid UTC timestamp.";
            return false;
        }

        if (!double.IsFinite(report.LatitudeDegrees) ||
            report.LatitudeDegrees is < -90 or > 90)
        {
            error = "LatitudeDegrees must be between -90 and 90.";
            return false;
        }

        if (!double.IsFinite(report.LongitudeDegrees) ||
            report.LongitudeDegrees is < -180 or > 180)
        {
            error = "LongitudeDegrees must be between -180 and 180.";
            return false;
        }

        if (!double.IsFinite(report.AltitudeMeters) || report.AltitudeMeters < 0)
        {
            error = "AltitudeMeters must be a finite, non-negative number.";
            return false;
        }

        if (!double.IsFinite(report.GroundSpeedMetersPerSecond) ||
            report.GroundSpeedMetersPerSecond < 0)
        {
            error = "GroundSpeedMetersPerSecond must be a finite, non-negative number.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
