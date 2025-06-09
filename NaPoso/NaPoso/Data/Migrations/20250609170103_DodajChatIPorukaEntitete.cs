using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaPoso.Migrations
{
    /// <inheritdoc />
    public partial class DodajChatIPorukaEntitete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Chat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Korisnik1Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Korisnik2Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OglasId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chat", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chat_Korisnik_Korisnik1Id",
                        column: x => x.Korisnik1Id,
                        principalTable: "Korisnik",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Chat_Korisnik_Korisnik2Id",
                        column: x => x.Korisnik2Id,
                        principalTable: "Korisnik",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Chat_Oglas_OglasId",
                        column: x => x.OglasId,
                        principalTable: "Oglas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Poruka",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChatId = table.Column<int>(type: "int", nullable: false),
                    PosiljaocId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Tekst = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PoslanoAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Poruka", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Poruka_Chat_ChatId",
                        column: x => x.ChatId,
                        principalTable: "Chat",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Poruka_Korisnik_PosiljaocId",
                        column: x => x.PosiljaocId,
                        principalTable: "Korisnik",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Chat_Korisnik1Id",
                table: "Chat",
                column: "Korisnik1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Chat_Korisnik2Id",
                table: "Chat",
                column: "Korisnik2Id");

            migrationBuilder.CreateIndex(
                name: "IX_Chat_OglasId",
                table: "Chat",
                column: "OglasId");

            migrationBuilder.CreateIndex(
                name: "IX_Poruka_ChatId",
                table: "Poruka",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_Poruka_PosiljaocId",
                table: "Poruka",
                column: "PosiljaocId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Poruka");

            migrationBuilder.DropTable(
                name: "Chat");
        }
    }
}
