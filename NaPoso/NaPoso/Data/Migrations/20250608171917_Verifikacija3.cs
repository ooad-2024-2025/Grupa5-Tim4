using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaPoso.Migrations
{
    /// <inheritdoc />
    public partial class Verifikacija3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Verified",
                table: "Korisnik",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
           
        }
    }
}
