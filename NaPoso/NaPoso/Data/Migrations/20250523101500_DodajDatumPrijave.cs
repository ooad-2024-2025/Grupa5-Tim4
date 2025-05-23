using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaPoso.Data.Migrations
{
    /// <inheritdoc />
    public partial class DodajDatumPrijave : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OglasKorisnikU_Oglas_oglasId",
                table: "OglasKorisnikU");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OglasKorisnikU",
                table: "OglasKorisnikU");

            migrationBuilder.RenameTable(
                name: "OglasKorisnikU",
                newName: "OglasKorisnik");

            migrationBuilder.RenameColumn(
                name: "oglasId",
                table: "OglasKorisnik",
                newName: "OglasId");

            migrationBuilder.RenameIndex(
                name: "IX_OglasKorisnikU_oglasId",
                table: "OglasKorisnik",
                newName: "IX_OglasKorisnik_OglasId");

            migrationBuilder.AlterColumn<string>(
                name: "KorisnikId",
                table: "OglasKorisnik",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "DatumPrijave",
                table: "OglasKorisnik",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddPrimaryKey(
                name: "PK_OglasKorisnik",
                table: "OglasKorisnik",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_OglasKorisnik_KorisnikId",
                table: "OglasKorisnik",
                column: "KorisnikId");

            migrationBuilder.AddForeignKey(
                name: "FK_OglasKorisnik_AspNetUsers_KorisnikId",
                table: "OglasKorisnik",
                column: "KorisnikId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OglasKorisnik_Oglas_OglasId",
                table: "OglasKorisnik",
                column: "OglasId",
                principalTable: "Oglas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OglasKorisnik_AspNetUsers_KorisnikId",
                table: "OglasKorisnik");

            migrationBuilder.DropForeignKey(
                name: "FK_OglasKorisnik_Oglas_OglasId",
                table: "OglasKorisnik");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OglasKorisnik",
                table: "OglasKorisnik");

            migrationBuilder.DropIndex(
                name: "IX_OglasKorisnik_KorisnikId",
                table: "OglasKorisnik");

            migrationBuilder.DropColumn(
                name: "DatumPrijave",
                table: "OglasKorisnik");

            migrationBuilder.RenameTable(
                name: "OglasKorisnik",
                newName: "OglasKorisnikU");

            migrationBuilder.RenameColumn(
                name: "OglasId",
                table: "OglasKorisnikU",
                newName: "oglasId");

            migrationBuilder.RenameIndex(
                name: "IX_OglasKorisnik_OglasId",
                table: "OglasKorisnikU",
                newName: "IX_OglasKorisnikU_oglasId");

            migrationBuilder.AlterColumn<string>(
                name: "KorisnikId",
                table: "OglasKorisnikU",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OglasKorisnikU",
                table: "OglasKorisnikU",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OglasKorisnikU_Oglas_oglasId",
                table: "OglasKorisnikU",
                column: "oglasId",
                principalTable: "Oglas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
