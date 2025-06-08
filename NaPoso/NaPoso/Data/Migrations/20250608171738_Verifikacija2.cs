using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaPoso.Migrations
{
    /// <inheritdoc />
    public partial class Verifikacija2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Verifikovan",
                table: "Korisnik",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
