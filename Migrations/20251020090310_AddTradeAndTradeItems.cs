using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarterSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddTradeAndTradeItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TradeRequests_Items_OfferedItemId",
                table: "TradeRequests");

            migrationBuilder.AddForeignKey(
                name: "FK_TradeRequests_Items_OfferedItemId",
                table: "TradeRequests",
                column: "OfferedItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TradeRequests_Items_OfferedItemId",
                table: "TradeRequests");

            migrationBuilder.AddForeignKey(
                name: "FK_TradeRequests_Items_OfferedItemId",
                table: "TradeRequests",
                column: "OfferedItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
