using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarterSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddTradeSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "IsTradable",
                table: "Items");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Trades",
                newName: "DateRequested");

            migrationBuilder.RenameColumn(
                name: "FromRequester",
                table: "TradeItems",
                newName: "IsOfferedByRequester");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Items",
                newName: "Name");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Trades",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DateRequested",
                table: "Trades",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "IsOfferedByRequester",
                table: "TradeItems",
                newName: "FromRequester");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Items",
                newName: "Title");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Trades",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Trades",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Items",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTradable",
                table: "Items",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
