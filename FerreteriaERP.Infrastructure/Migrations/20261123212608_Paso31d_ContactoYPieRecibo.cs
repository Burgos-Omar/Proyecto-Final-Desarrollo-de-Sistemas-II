using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FerreteriaERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Paso31d_ContactoYPieRecibo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Direccion",
                table: "configuraciones_negocio",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MensajePieRecibo",
                table: "configuraciones_negocio",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "configuraciones_negocio",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Direccion",
                table: "configuraciones_negocio");

            migrationBuilder.DropColumn(
                name: "MensajePieRecibo",
                table: "configuraciones_negocio");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "configuraciones_negocio");
        }
    }
}
