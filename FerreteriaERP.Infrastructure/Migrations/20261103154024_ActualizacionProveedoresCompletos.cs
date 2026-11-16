using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FerreteriaERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ActualizacionProveedoresCompletos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CiContacto",
                table: "Proveedores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DireccionAlmacen",
                table: "Proveedores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailInstitucional",
                table: "Proveedores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormasPagoAceptadas",
                table: "Proveedores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicenciaFuncionamiento",
                table: "Proveedores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MonedaFacturacion",
                table: "Proveedores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nit",
                table: "Proveedores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreComercial",
                table: "Proveedores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PoliticaFletes",
                table: "Proveedores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PoliticaGarantiaYCambios",
                table: "Proveedores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RutaDocumentoAdjunto",
                table: "Proveedores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelefonosContacto",
                table: "Proveedores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TiemposEntrega",
                table: "Proveedores",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CiContacto",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "DireccionAlmacen",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "EmailInstitucional",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "FormasPagoAceptadas",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "LicenciaFuncionamiento",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "MonedaFacturacion",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "Nit",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "NombreComercial",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "PoliticaFletes",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "PoliticaGarantiaYCambios",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "RutaDocumentoAdjunto",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "TelefonosContacto",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "TiemposEntrega",
                table: "Proveedores");
        }
    }
}
