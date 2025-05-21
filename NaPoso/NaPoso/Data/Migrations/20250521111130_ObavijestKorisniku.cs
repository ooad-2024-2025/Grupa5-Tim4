using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaPoso.Data.Migrations
{
    /// <inheritdoc />
    public partial class ObavijestKorisniku : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ObavijestKorisniku",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KorisnikId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    obavijestId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObavijestKorisniku", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObavijestKorisniku_Obavijest_obavijestId",
                        column: x => x.obavijestId,
                        principalTable: "Obavijest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OglasKorisnik",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KorisnikId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    oglasId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OglasKorisnik", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OglasKorisnik_Oglas_oglasId",
                        column: x => x.oglasId,
                        principalTable: "Oglas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ObavijestKorisniku_obavijestId",
                table: "ObavijestKorisniku",
                column: "obavijestId");

            migrationBuilder.CreateIndex(
                name: "IX_OglasKorisnik_oglasId",
                table: "OglasKorisnik",
                column: "oglasId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ObavijestKorisniku");

            migrationBuilder.DropTable(
                name: "OglasKorisnik");
        }
    }
}
