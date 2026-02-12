using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grovetracks.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddWordIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_quickdraw_doodles_word",
                table: "quickdraw_doodles",
                column: "word");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_quickdraw_doodles_word",
                table: "quickdraw_doodles");
        }
    }
}
