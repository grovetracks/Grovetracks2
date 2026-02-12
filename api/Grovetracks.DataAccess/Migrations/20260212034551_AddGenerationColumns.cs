using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grovetracks.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddGenerationColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "generation_method",
                table: "seed_compositions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_composition_ids",
                table: "seed_compositions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_type",
                table: "seed_compositions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "curated");

            migrationBuilder.CreateIndex(
                name: "IX_seed_compositions_source_type",
                table: "seed_compositions",
                column: "source_type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_seed_compositions_source_type",
                table: "seed_compositions");

            migrationBuilder.DropColumn(
                name: "generation_method",
                table: "seed_compositions");

            migrationBuilder.DropColumn(
                name: "source_composition_ids",
                table: "seed_compositions");

            migrationBuilder.DropColumn(
                name: "source_type",
                table: "seed_compositions");
        }
    }
}
