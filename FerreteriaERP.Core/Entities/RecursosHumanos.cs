using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace FerreteriaERP.Core.Entities
{
    public class Rol
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        
        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }

    public class Sucursal
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Direccion { get; set; }
        public bool EsCentral { get; set; }
        
        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
        public ICollection<StockSucursal> Stocks { get; set; } = new List<StockSucursal>();
        public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
        public ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();
    }

    public class Usuario
    {
        public int Id { get; set; }
        public string Ci { get; set; } = null!;
        public string NombreCompleto { get; set; } = null!;
        public string PasswordHash { get; set; } = string.Empty;
        public string? Celular { get; set; }
        public string? Direccion { get; set; }
        public decimal Salario { get; set; }
        public DateTime FechaInicio { get; set; }
        public string? FotoPath { get; set; }
        public string Estado { get; set; } = "Activo"; // Activo, Inhabilitado

        public int IdRol { get; set; }
        public Rol Rol { get; set; } = null!;

        public int IdSucursal { get; set; }
        public Sucursal Sucursal { get; set; } = null!;
    }

    public class Empleado
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public string Apellidos { get; set; } = null!;
        public string Ci { get; set; } = null!;
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public decimal Salario { get; set; }
        public string Puesto { get; set; } = "Vendedor"; // Vendedor, Cajero, Almacenero, Chofer, Encargado
        public string? ImagenUrl { get; set; }

        public int IdSucursal { get; set; }
        public Sucursal Sucursal { get; set; } = null!;

        public bool EstaActivo { get; set; } = true;

        public ICollection<Venta> Ventas { get; set; } = new List<Venta>();

        // Propiedades auxiliares de presentación para tarjetas y POS (no mapeadas en BD)
        [NotMapped]
        public string NombreCompleto => $"{Nombre} {Apellidos}".Trim();

        [NotMapped]
        public string Iniciales
        {
            get
            {
                var n = !string.IsNullOrWhiteSpace(Nombre) ? char.ToUpperInvariant(Nombre.Trim()[0]).ToString() : "E";
                var a = !string.IsNullOrWhiteSpace(Apellidos) ? char.ToUpperInvariant(Apellidos.Trim()[0]).ToString() : "M";
                return $"{n}{a}";
            }
        }

        [NotMapped]
        public string SucursalNombreMostrar => Sucursal?.Nombre ?? $"Sucursal #{IdSucursal}";

        [NotMapped]
        public string TelefonoMostrar => string.IsNullOrWhiteSpace(Telefono) ? "Sin teléfono" : Telefono;

        [NotMapped]
        public string DireccionMostrar => string.IsNullOrWhiteSpace(Direccion) ? "Sin dirección registrada" : Direccion;

        [NotMapped]
        public string SalarioFormateado => $"Bs. {Salario:N2}";

        [NotMapped]
        public string EstadoTexto => EstaActivo ? "Activo" : "Inhabilitado";

        [NotMapped]
        public string TextoBotonEstado => EstaActivo ? "Inhabilitar" : "Habilitar";

        public override string ToString() => $"{NombreCompleto} ({Puesto})";
    }
}
