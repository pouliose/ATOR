using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATOR.Consumer.Reports.Migrations
{
    /// <inheritdoc />
    public partial class InitialTelemetrySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS current_tracks (
                    source_id text NOT NULL,
                    track_id text NOT NULL,
                    timestamp_utc timestamp with time zone NOT NULL,
                    latitude_degrees double precision NOT NULL,
                    longitude_degrees double precision NOT NULL,
                    altitude_meters double precision NOT NULL,
                    ground_speed_meters_per_second double precision NOT NULL,
                    CONSTRAINT "PK_current_tracks" PRIMARY KEY (source_id, track_id)
                );

                CREATE TABLE IF NOT EXISTS telemetry_reports (
                    source_id text NOT NULL,
                    track_id text NOT NULL,
                    timestamp_utc timestamp with time zone NOT NULL,
                    latitude_degrees double precision NOT NULL,
                    longitude_degrees double precision NOT NULL,
                    altitude_meters double precision NOT NULL,
                    ground_speed_meters_per_second double precision NOT NULL,
                    CONSTRAINT "PK_telemetry_reports"
                        PRIMARY KEY (source_id, track_id, timestamp_utc)
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "current_tracks");

            migrationBuilder.DropTable(
                name: "telemetry_reports");
        }
    }
}
