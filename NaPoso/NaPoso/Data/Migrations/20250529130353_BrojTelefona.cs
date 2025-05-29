using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaPoso.Migrations
{
    /// <inheritdoc />
    public partial class BrojTelefona : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OglasKorisnik_AspNetUsers_KorisnikId",
                table: "OglasKorisnik");

            migrationBuilder.AlterColumn<string>(
                name: "TipPosla",
                table: "Oglas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Opis",
                table: "Oglas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Naslov",
                table: "Oglas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Lokacija",
                table: "Oglas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OglasKorisnik_Korisnik_KorisnikId",
                table: "OglasKorisnik",
                column: "KorisnikId",
                principalTable: "Korisnik",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OglasKorisnik_Korisnik_KorisnikId",
                table: "OglasKorisnik");

            migrationBuilder.AlterColumn<string>(
                name: "TipPosla",
                table: "Oglas",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Opis",
                table: "Oglas",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Naslov",
                table: "Oglas",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Lokacija",
                table: "Oglas",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddForeignKey(
                name: "FK_OglasKorisnik_AspNetUsers_KorisnikId",
                table: "OglasKorisnik",
                column: "KorisnikId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
