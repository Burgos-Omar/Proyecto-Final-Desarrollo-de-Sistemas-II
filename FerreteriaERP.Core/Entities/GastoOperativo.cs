using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace FerreteriaERP.Core.Entities;

/// <summary>
/// Representa un gasto operativo o extra de la ferretería (Alquiler, Salarios, Luz/Agua, Impuestos, Otros)
/// utilizado en el módulo de Inteligencia Gerencial para calcular la Ganancia Líquida Neta.
/// </summary>
public class GastoOperativo
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string Categoria { get; set; } = "Otros";
    public string Descripcion { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public int IdSucursal { get; set; }
    public Sucursal? Sucursal { get; set; }

    [NotMapped]
    public string FechaTexto => (Fecha.Kind == DateTimeKind.Utc ? Fecha.ToLocalTime() : Fecha).ToString("dd/MM/yyyy");

    [NotMapped]
    public string MontoTexto => $"Bs. {Monto:N2}";
}
