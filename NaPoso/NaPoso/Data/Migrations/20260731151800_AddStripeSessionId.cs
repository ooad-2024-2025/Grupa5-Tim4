using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaPoso.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeSessionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add StripeSessionId column (nullable)
            migrationBuilder.AddColumn<string>(
                name: "StripeSessionId",
                table: "PaymentTransaction",
                type: "text",
                nullable: true);

            // Create unique filtered index on StripeSessionId
            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_StripeSessionId",
                table: "PaymentTransaction",
                column: "StripeSessionId",
                unique: true,
                filter: "\"StripeSessionId\" IS NOT NULL");

            // Drop the old non-filtered unique index on StripeEventId
            migrationBuilder.DropIndex(
                name: "IX_PaymentTransaction_StripeEventId",
                table: "PaymentTransaction");

            // Make StripeEventId nullable
            migrationBuilder.AlterColumn<string>(
                name: "StripeEventId",
                table: "PaymentTransaction",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            // Nullify empty StripeEventId values to avoid unique constraint issues
            migrationBuilder.Sql(
                "UPDATE \"PaymentTransaction\" SET \"StripeEventId\" = NULL WHERE \"StripeEventId\" = ''");

            // Create new filtered unique index on StripeEventId
            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_StripeEventId",
                table: "PaymentTransaction",
                column: "StripeEventId",
                unique: true,
                filter: "\"StripeEventId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert StripeEventId index
            migrationBuilder.DropIndex(
                name: "IX_PaymentTransaction_StripeEventId",
                table: "PaymentTransaction");

            // Make StripeEventId non-nullable again
            migrationBuilder.Sql(
                "UPDATE \"PaymentTransaction\" SET \"StripeEventId\" = '' WHERE \"StripeEventId\" IS NULL");

            migrationBuilder.AlterColumn<string>(
                name: "StripeEventId",
                table: "PaymentTransaction",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_StripeEventId",
                table: "PaymentTransaction",
                column: "StripeEventId",
                unique: true);

            // Remove StripeSessionId
            migrationBuilder.DropIndex(
                name: "IX_PaymentTransaction_StripeSessionId",
                table: "PaymentTransaction");

            migrationBuilder.DropColumn(
                name: "StripeSessionId",
                table: "PaymentTransaction");
        }
    }
}
