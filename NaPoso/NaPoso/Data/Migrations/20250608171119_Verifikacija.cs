using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaPoso.Migrations
{
    /// <inheritdoc />
    public partial class Verifikacija : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KlijentId",
                table: "OdobreniDokumenti",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
