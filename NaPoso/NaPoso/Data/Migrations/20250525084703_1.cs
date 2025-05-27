using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaPoso.Data.Migrations
{
    /// <inheritdoc />
    public partial class _1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chat_AspNetUsers_KlijentId",
                table: "Chat");

            migrationBuilder.AddColumn<int>(
                name: "chatId",
                table: "Message",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "KlijentId",
                table: "Chat",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Message_chatId",
                table: "Message",
                column: "chatId");

            migrationBuilder.AddForeignKey(
                name: "FK_Chat_AspNetUsers_KlijentId",
                table: "Chat",
                column: "KlijentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Message_Chat_chatId",
                table: "Message",
                column: "chatId",
                principalTable: "Chat",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chat_AspNetUsers_KlijentId",
                table: "Chat");

            migrationBuilder.DropForeignKey(
                name: "FK_Message_Chat_chatId",
                table: "Message");

            migrationBuilder.DropIndex(
                name: "IX_Message_chatId",
                table: "Message");

            migrationBuilder.DropColumn(
                name: "chatId",
                table: "Message");

            migrationBuilder.AlterColumn<string>(
                name: "KlijentId",
                table: "Chat",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddForeignKey(
                name: "FK_Chat_AspNetUsers_KlijentId",
                table: "Chat",
                column: "KlijentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
