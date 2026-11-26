using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FerreteriaERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Paso30_OrdenesCompra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ordenes_compra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NumeroOrden = table.Column<string>(type: "text", nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IdProveedor = table.Column<int>(type: "integer", nullable: false),
                    IdSucursal = table.Column<int>(type: "integer", nullable: false),
                    IdUsuario = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    TotalEstimado = table.Column<decimal>(type: "numeric", nullable: false),
                    Observaciones = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ordenes_compra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ordenes_compra_Proveedores_IdProveedor",
                        column: x => x.IdProveedor,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ordenes_compra_Sucursales_IdSucursal",
                        column: x => x.IdSucursal,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ordenes_compra_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "detalles_orden_compra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdOrdenCompra = table.Column<int>(type: "integer", nullable: false),
                    IdProducto = table.Column<int>(type: "integer", nullable: false),
                    CantidadSolicitada = table.Column<int>(type: "integer", nullable: false),
                    CostoUnitarioEstimado = table.Column<decimal>(type: "numeric", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detalles_orden_compra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_detalles_orden_compra_Productos_IdProducto",
                        column: x => x.IdProducto,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_detalles_orden_compra_ordenes_compra_IdOrdenCompra",
                        column: x => x.IdOrdenCompra,
                        principalTable: "ordenes_compra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_detalles_orden_compra_IdOrdenCompra",
                table: "detalles_orden_compra",
                column: "IdOrdenCompra");

            migrationBuilder.CreateIndex(
                name: "IX_detalles_orden_compra_IdProducto",
                table: "detalles_orden_compra",
                column: "IdProducto");

            migrationBuilder.CreateIndex(
                name: "IX_ordenes_compra_IdProveedor",
                table: "ordenes_compra",
                column: "IdProveedor");

            migrationBuilder.CreateIndex(
                name: "IX_ordenes_compra_IdSucursal",
                table: "ordenes_compra",
                column: "IdSucursal");

            migrationBuilder.CreateIndex(
                name: "IX_ordenes_compra_IdUsuario",
                table: "ordenes_compra",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_ordenes_compra_NumeroOrden",
                table: "ordenes_compra",
                column: "NumeroOrden",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "detalles_orden_compra");

            migrationBuilder.DropTable(
                name: "ordenes_compra");
        }
    }
}
