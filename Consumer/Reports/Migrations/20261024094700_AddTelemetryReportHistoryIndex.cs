using ATOR.Reports;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATOR.Consumer.Reports.Migrations;

[DbContext(typeof(TelemetryDbContext))]
[Migration("20261024094700_AddTelemetryReportHistoryIndex")]
public partial class AddTelemetryReportHistoryIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_telemetry_reports_track_id_timestamp_utc_source_id",
            table: "telemetry_reports",
            columns: new[] { "track_id", "timestamp_utc", "source_id" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_telemetry_reports_track_id_timestamp_utc_source_id",
            table: "telemetry_reports");
    }
}
