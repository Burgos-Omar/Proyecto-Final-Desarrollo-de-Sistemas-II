using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace FerreteriaERP.Core.Entities
{
    public class Promocion
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
        public DateTime FechaFin { get; set; } = DateTime.UtcNow.AddDays(30);
        public string? ImagenFondoUrl { get; set; }
        public bool EstaActiva { get; set; } = true;

        [NotMapped]
        public string VigenciaTexto =>
            FechaFin <= FechaInicio
                ? "Sin fecha de vigencia"
                : $"Vigente del {FechaInicio.ToLocalTime():dd/MM/yyyy} al {FechaFin.ToLocalTime():dd/MM/yyyy}";
    }
}

