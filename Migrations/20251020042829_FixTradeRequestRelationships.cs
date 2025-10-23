using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarterSystem.Migrations
{
    /// <inheritdoc />
    public partial class FixTradeRequestRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TradeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequesterId = table.Column<int>(type: "int", nullable: false),
                    ReceiverId = table.Column<int>(type: "int", nullable: false),
                    OfferedItemId = table.Column<int>(type: "int", nullable: false),
                    RequestedItemId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradeRequests_Items_OfferedItemId",
                        column: x => x.OfferedItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TradeRequests_Items_RequestedItemId",
                        column: x => x.RequestedItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TradeRequests_Users_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TradeRequests_Users_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TradeRequests_OfferedItemId",
                table: "TradeRequests",
                column: "OfferedItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeRequests_ReceiverId",
                table: "TradeRequests",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeRequests_RequestedItemId",
                table: "TradeRequests",
                column: "RequestedItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeRequests_RequesterId",
                table: "TradeRequests",
                column: "RequesterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TradeRequests");
        }
    }
}
