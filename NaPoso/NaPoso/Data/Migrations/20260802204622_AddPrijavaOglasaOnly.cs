using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NaPoso.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPrijavaOglasaOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
                name: "PrijavaOglasa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OglasId = table.Column<int>(type: "integer", nullable: false),
                    PrijavioKorisnikId = table.Column<string>(type: "text", nullable: false),
                    Razlog = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DatumPrijave = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    JeRijeseno = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrijavaOglasa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrijavaOglasa_Korisnik_PrijavioKorisnikId",
                        column: x => x.PrijavioKorisnikId,
                        principalTable: "Korisnik",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrijavaOglasa_Oglas_OglasId",
                        column: x => x.OglasId,
                        principalTable: "Oglas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });



            migrationBuilder.CreateIndex(
                name: "IX_PrijavaOglasa_OglasId",
                table: "PrijavaOglasa",
                column: "OglasId");

            migrationBuilder.CreateIndex(
                name: "IX_PrijavaOglasa_PrijavioKorisnikId",
                table: "PrijavaOglasa",
                column: "PrijavioKorisnikId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrijavaOglasa");
        }
    }
}
