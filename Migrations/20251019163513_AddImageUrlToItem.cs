using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarterSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUrlToItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DateRequested",
                table: "Trades",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "IsOfferedByRequester",
                table: "TradeItems",
                newName: "IsOfferedItem");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Items",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Items");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Trades",
                newName: "DateRequested");

            migrationBuilder.RenameColumn(
                name: "IsOfferedItem",
                table: "TradeItems",
                newName: "IsOfferedByRequester");
        }
    }
}
