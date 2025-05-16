using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaPoso.Data.Migrations
{
    /// <inheritdoc />
    public partial class Recenzija : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Obavijest",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KorisnikId = table.Column<int>(type: "int", nullable: false),
                    Sadrzaj = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VrijemeSlanja = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tip = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Obavijest", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recenzija",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ocjena = table.Column<int>(type: "int", nullable: false),
                    Sadrzaj = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recenzija", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Oglas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KlijentId = table.Column<int>(type: "int", nullable: false),
                    RadnikId = table.Column<int>(type: "int", nullable: false),
                    Opis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lokacija = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipPosla = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CijenaPosla = table.Column<double>(type: "float", nullable: false),
                    Naslov = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecenzijaId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Oglas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Oglas_Recenzija_RecenzijaId",
                        column: x => x.RecenzijaId,
                        principalTable: "Recenzija",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Oglas_RecenzijaId",
                table: "Oglas",
                column: "RecenzijaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Obavijest");

            migrationBuilder.DropTable(
                name: "Oglas");

            migrationBuilder.DropTable(
                name: "Recenzija");
        }
    }
}
