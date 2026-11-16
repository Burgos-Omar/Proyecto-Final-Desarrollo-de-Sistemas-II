using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FerreteriaERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EstabilizarInicioYCargas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaRegistro",
                table: "cargas_entrantes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "IdSucursal",
                table: "cargas_entrantes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoTotal",
                table: "cargas_entrantes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_cargas_entrantes_IdSucursal",
                table: "cargas_entrantes",
                column: "IdSucursal");

            migrationBuilder.AddForeignKey(
                name: "FK_cargas_entrantes_Sucursales_IdSucursal",
                table: "cargas_entrantes",
                column: "IdSucursal",
                principalTable: "Sucursales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cargas_entrantes_Sucursales_IdSucursal",
                table: "cargas_entrantes");

            migrationBuilder.DropIndex(
                name: "IX_cargas_entrantes_IdSucursal",
                table: "cargas_entrantes");

            migrationBuilder.DropColumn(
                name: "FechaRegistro",
                table: "cargas_entrantes");

            migrationBuilder.DropColumn(
                name: "IdSucursal",
                table: "cargas_entrantes");

            migrationBuilder.DropColumn(
                name: "MontoTotal",
                table: "cargas_entrantes");
        }
    }
}
