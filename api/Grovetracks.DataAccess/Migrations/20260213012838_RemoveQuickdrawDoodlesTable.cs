using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grovetracks.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveQuickdrawDoodlesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quickdraw_doodles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quickdraw_doodles",
                columns: table => new
                {
                    key_id = table.Column<string>(type: "text", nullable: false),
                    country_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    drawing_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    recognized = table.Column<bool>(type: "boolean", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    word = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quickdraw_doodles", x => x.key_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_quickdraw_doodles_word",
                table: "quickdraw_doodles",
                column: "word");
        }
    }
}
