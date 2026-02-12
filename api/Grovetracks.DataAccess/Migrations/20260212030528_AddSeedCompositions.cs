using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grovetracks.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSeedCompositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "seed_compositions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    word = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_key_id = table.Column<string>(type: "text", nullable: false),
                    quality_score = table.Column<double>(type: "double precision", nullable: false),
                    stroke_count = table.Column<int>(type: "integer", nullable: false),
                    total_point_count = table.Column<int>(type: "integer", nullable: false),
                    composition_json = table.Column<string>(type: "jsonb", nullable: false),
                    curated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seed_compositions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_seed_compositions_quality_score",
                table: "seed_compositions",
                column: "quality_score");

            migrationBuilder.CreateIndex(
                name: "IX_seed_compositions_word",
                table: "seed_compositions",
                column: "word");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "seed_compositions");
        }
    }
}
