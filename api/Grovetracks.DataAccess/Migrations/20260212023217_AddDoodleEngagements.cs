using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grovetracks.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddDoodleEngagements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "doodle_engagements",
                columns: table => new
                {
                    key_id = table.Column<string>(type: "text", nullable: false),
                    score = table.Column<double>(type: "double precision", nullable: false),
                    engaged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_doodle_engagements", x => x.key_id);
                });

            migrationBuilder.CreateTable(
                name: "quickdraw_simple_doodles",
                columns: table => new
                {
                    key_id = table.Column<string>(type: "text", nullable: false),
                    word = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    country_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    recognized = table.Column<bool>(type: "boolean", nullable: false),
                    drawing = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quickdraw_simple_doodles", x => x.key_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_quickdraw_simple_doodles_word",
                table: "quickdraw_simple_doodles",
                column: "word");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "doodle_engagements");

            migrationBuilder.DropTable(
                name: "quickdraw_simple_doodles");
        }
    }
}
