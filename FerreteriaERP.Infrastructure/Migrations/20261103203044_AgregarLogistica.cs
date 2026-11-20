using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FerreteriaERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarLogistica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cargas_entrantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdProveedor = table.Column<int>(type: "integer", nullable: false),
                    IdProducto = table.Column<int>(type: "integer", nullable: true),
                    CantidadUnidades = table.Column<int>(type: "integer", nullable: false),
                    NumeroGuia = table.Column<string>(type: "text", nullable: false),
                    EmpresaTransporte = table.Column<string>(type: "text", nullable: false),
                    CostoFleteDestino = table.Column<decimal>(type: "numeric", nullable: false),
                    FechaEstimadaLlegada = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    Observaciones = table.Column<string>(type: "text", nullable: true),
                    StockActualizado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cargas_entrantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cargas_entrantes_Productos_IdProducto",
                        column: x => x.IdProducto,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_cargas_entrantes_Proveedores_IdProveedor",
                        column: x => x.IdProveedor,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ordenes_despacho",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdVenta = table.Column<int>(type: "integer", nullable: false),
                    DireccionEntrega = table.Column<string>(type: "text", nullable: false),
                    VehiculoChofer = table.Column<string>(type: "text", nullable: false),
                    CostoFleteFerreteria = table.Column<decimal>(type: "numeric", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    NombreReceptorObra = table.Column<string>(type: "text", nullable: true),
                    FechaEntrega = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UnidadesTotales = table.Column<int>(type: "integer", nullable: false),
                    UnidadesEntregadas = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ordenes_despacho", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ordenes_despacho_Ventas_IdVenta",
                        column: x => x.IdVenta,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cargas_entrantes_IdProducto",
                table: "cargas_entrantes",
                column: "IdProducto");

            migrationBuilder.CreateIndex(
                name: "IX_cargas_entrantes_IdProveedor",
                table: "cargas_entrantes",
                column: "IdProveedor");

            migrationBuilder.CreateIndex(
                name: "IX_ordenes_despacho_IdVenta",
                table: "ordenes_despacho",
                column: "IdVenta");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cargas_entrantes");

            migrationBuilder.DropTable(
                name: "ordenes_despacho");
        }
    }
}
