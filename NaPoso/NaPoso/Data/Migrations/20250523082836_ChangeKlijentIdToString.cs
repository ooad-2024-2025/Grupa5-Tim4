using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaPoso.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeKlijentIdToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Oglas_Recenzija_RecenzijaId",
                table: "Oglas");

            migrationBuilder.DropForeignKey(
                name: "FK_OglasKorisnik_Oglas_oglasId",
                table: "OglasKorisnik");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OglasKorisnik",
                table: "OglasKorisnik");

            migrationBuilder.RenameTable(
                name: "OglasKorisnik",
                newName: "OglasKorisnikU");

            migrationBuilder.RenameIndex(
                name: "IX_OglasKorisnik_oglasId",
                table: "OglasKorisnikU",
                newName: "IX_OglasKorisnikU_oglasId");

            migrationBuilder.AlterColumn<string>(
                name: "TipPosla",
                table: "Oglas",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "RecenzijaId",
                table: "Oglas",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "RadnikId",
                table: "Oglas",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Opis",
                table: "Oglas",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Naslov",
                table: "Oglas",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Lokacija",
                table: "Oglas",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "KlijentId",
                table: "Oglas",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OglasKorisnikU",
                table: "OglasKorisnikU",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Oglas_Recenzija_RecenzijaId",
                table: "Oglas",
                column: "RecenzijaId",
                principalTable: "Recenzija",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OglasKorisnikU_Oglas_oglasId",
                table: "OglasKorisnikU",
                column: "oglasId",
                principalTable: "Oglas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Oglas_Recenzija_RecenzijaId",
                table: "Oglas");

            migrationBuilder.DropForeignKey(
                name: "FK_OglasKorisnikU_Oglas_oglasId",
                table: "OglasKorisnikU");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OglasKorisnikU",
                table: "OglasKorisnikU");

            migrationBuilder.RenameTable(
                name: "OglasKorisnikU",
                newName: "OglasKorisnik");

            migrationBuilder.RenameIndex(
                name: "IX_OglasKorisnikU_oglasId",
                table: "OglasKorisnik",
                newName: "IX_OglasKorisnik_oglasId");

            migrationBuilder.AlterColumn<string>(
                name: "TipPosla",
                table: "Oglas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RecenzijaId",
                table: "Oglas",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RadnikId",
                table: "Oglas",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Opis",
                table: "Oglas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Naslov",
                table: "Oglas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Lokacija",
                table: "Oglas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "KlijentId",
                table: "Oglas",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OglasKorisnik",
                table: "OglasKorisnik",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Oglas_Recenzija_RecenzijaId",
                table: "Oglas",
                column: "RecenzijaId",
                principalTable: "Recenzija",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OglasKorisnik_Oglas_oglasId",
                table: "OglasKorisnik",
                column: "oglasId",
                principalTable: "Oglas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
