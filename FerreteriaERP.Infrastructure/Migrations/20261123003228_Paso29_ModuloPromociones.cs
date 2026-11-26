using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FerreteriaERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Paso29_ModuloPromociones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Alcance",
                table: "Promociones",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CantidadMinima",
                table: "Promociones",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IdCategoria",
                table: "Promociones",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdProducto",
                table: "Promociones",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdSucursal",
                table: "Promociones",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nombre",
                table: "Promociones",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TipoPromocion",
                table: "Promociones",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ValorDescuento",
                table: "Promociones",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "IdPromocion",
                table: "detalle_ventas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoDescuento",
                table: "detalle_ventas",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "IdPromocion",
                table: "detalle_cotizaciones",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoDescuento",
                table: "detalle_cotizaciones",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Promociones_IdCategoria",
                table: "Promociones",
                column: "IdCategoria");

            migrationBuilder.CreateIndex(
                name: "IX_Promociones_IdProducto",
                table: "Promociones",
                column: "IdProducto");

            migrationBuilder.CreateIndex(
                name: "IX_Promociones_IdSucursal",
                table: "Promociones",
                column: "IdSucursal");

            migrationBuilder.CreateIndex(
                name: "IX_detalle_ventas_IdPromocion",
                table: "detalle_ventas",
                column: "IdPromocion");

            migrationBuilder.CreateIndex(
                name: "IX_detalle_cotizaciones_IdPromocion",
                table: "detalle_cotizaciones",
                column: "IdPromocion");

            migrationBuilder.AddForeignKey(
                name: "FK_detalle_cotizaciones_Promociones_IdPromocion",
                table: "detalle_cotizaciones",
                column: "IdPromocion",
                principalTable: "Promociones",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_detalle_ventas_Promociones_IdPromocion",
                table: "detalle_ventas",
                column: "IdPromocion",
                principalTable: "Promociones",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Promociones_Categorias_IdCategoria",
                table: "Promociones",
                column: "IdCategoria",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Promociones_Productos_IdProducto",
                table: "Promociones",
                column: "IdProducto",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Promociones_Sucursales_IdSucursal",
                table: "Promociones",
                column: "IdSucursal",
                principalTable: "Sucursales",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_detalle_cotizaciones_Promociones_IdPromocion",
                table: "detalle_cotizaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_detalle_ventas_Promociones_IdPromocion",
                table: "detalle_ventas");

            migrationBuilder.DropForeignKey(
                name: "FK_Promociones_Categorias_IdCategoria",
                table: "Promociones");

            migrationBuilder.DropForeignKey(
                name: "FK_Promociones_Productos_IdProducto",
                table: "Promociones");

            migrationBuilder.DropForeignKey(
                name: "FK_Promociones_Sucursales_IdSucursal",
                table: "Promociones");

            migrationBuilder.DropIndex(
                name: "IX_Promociones_IdCategoria",
                table: "Promociones");

            migrationBuilder.DropIndex(
                name: "IX_Promociones_IdProducto",
                table: "Promociones");

            migrationBuilder.DropIndex(
                name: "IX_Promociones_IdSucursal",
                table: "Promociones");

            migrationBuilder.DropIndex(
                name: "IX_detalle_ventas_IdPromocion",
                table: "detalle_ventas");

            migrationBuilder.DropIndex(
                name: "IX_detalle_cotizaciones_IdPromocion",
                table: "detalle_cotizaciones");

            migrationBuilder.DropColumn(
                name: "Alcance",
                table: "Promociones");

            migrationBuilder.DropColumn(
                name: "CantidadMinima",
                table: "Promociones");

            migrationBuilder.DropColumn(
                name: "IdCategoria",
                table: "Promociones");

            migrationBuilder.DropColumn(
                name: "IdProducto",
                table: "Promociones");

            migrationBuilder.DropColumn(
                name: "IdSucursal",
                table: "Promociones");

            migrationBuilder.DropColumn(
                name: "Nombre",
                table: "Promociones");

            migrationBuilder.DropColumn(
                name: "TipoPromocion",
                table: "Promociones");

            migrationBuilder.DropColumn(
                name: "ValorDescuento",
                table: "Promociones");

            migrationBuilder.DropColumn(
                name: "IdPromocion",
                table: "detalle_ventas");

            migrationBuilder.DropColumn(
                name: "MontoDescuento",
                table: "detalle_ventas");

            migrationBuilder.DropColumn(
                name: "IdPromocion",
                table: "detalle_cotizaciones");

            migrationBuilder.DropColumn(
                name: "MontoDescuento",
                table: "detalle_cotizaciones");
        }
    }
}
