using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATOR.Consumer.Reports.Migrations
{
    /// <inheritdoc />
    public partial class RenameCurrentTracksToLatestTracks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_current_tracks",
                table: "current_tracks");

            migrationBuilder.RenameTable(
                name: "current_tracks",
                newName: "latest_tracks");

            migrationBuilder.AddPrimaryKey(
                name: "PK_latest_tracks",
                table: "latest_tracks",
                columns: new[] { "source_id", "track_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_latest_tracks",
                table: "latest_tracks");

            migrationBuilder.RenameTable(
                name: "latest_tracks",
                newName: "current_tracks");

            migrationBuilder.AddPrimaryKey(
                name: "PK_current_tracks",
                table: "current_tracks",
                columns: new[] { "source_id", "track_id" });
        }
    }
}
