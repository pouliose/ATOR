using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ATOR.Consumer.Reports.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "track_alerts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type = table.Column<string>(type: "text", nullable: false),
                    source_id = table.Column<string>(type: "text", nullable: false),
                    track_id = table.Column<string>(type: "text", nullable: false),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    observed_timestamp_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deduplication_key = table.Column<string>(type: "text", nullable: false),
                    zone_id = table.Column<string>(type: "text", nullable: true),
                    details = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_track_alerts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_track_alerts_occurred_at_utc_id",
                table: "track_alerts",
                columns: new[] { "occurred_at_utc", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_track_alerts_type_source_track_observed_timestamp",
                table: "track_alerts",
                columns: new[] { "type", "source_id", "track_id", "observed_timestamp_utc", "deduplication_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "track_alerts");
        }
    }
}
