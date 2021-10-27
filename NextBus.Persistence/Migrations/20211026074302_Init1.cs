using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NextBus.Persistence.Migrations
{
    public partial class Init1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Buses_Drivers_DriverAppUserId",
                table: "Buses");

            migrationBuilder.DropIndex(
                name: "IX_Buses_DriverAppUserId",
                table: "Buses");

            migrationBuilder.DropColumn(
                name: "DriverAppUserId",
                table: "Buses");

            migrationBuilder.DropColumn(
                name: "DriverId",
                table: "Buses");

            migrationBuilder.AddColumn<Guid>(
                name: "BusId",
                table: "Drivers",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_BusId",
                table: "Drivers",
                column: "BusId");

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_Buses_BusId",
                table: "Drivers",
                column: "BusId",
                principalTable: "Buses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_Buses_BusId",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_BusId",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "BusId",
                table: "Drivers");

            migrationBuilder.AddColumn<string>(
                name: "DriverAppUserId",
                table: "Buses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DriverId",
                table: "Buses",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Buses_DriverAppUserId",
                table: "Buses",
                column: "DriverAppUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Buses_Drivers_DriverAppUserId",
                table: "Buses",
                column: "DriverAppUserId",
                principalTable: "Drivers",
                principalColumn: "AppUserId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
