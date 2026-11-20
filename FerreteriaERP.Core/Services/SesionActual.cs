using System;
using FerreteriaERP.Core.Entities;

namespace FerreteriaERP.Core.Services;

/// <summary>
/// Gestor estático de sesión activa con los datos del usuario autenticado, su rol y su sucursal.
/// </summary>
public static class SesionActual
{
    public static int IdUsuario { get; set; }
    public static string NombreCompleto { get; set; } = string.Empty;
    public static string Rol { get; set; } = string.Empty;
    public static int IdSucursal { get; set; }
    public static string NombreSucursal { get; set; } = string.Empty;

    public static bool EstaAutenticado => IdUsuario > 0;

    /// <summary>
    /// Indica si el usuario autenticado posee rol de Administrador.
    /// </summary>
    public static bool EsAdmin =>
        string.Equals(Rol?.Trim(), "Administrador", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Rol?.Trim(), "Admin", StringComparison.OrdinalIgnoreCase);

    public static void Establecer(Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        IdUsuario = usuario.Id;
        NombreCompleto = usuario.NombreCompleto;
        Rol = usuario.Rol?.Nombre ?? string.Empty;
        IdSucursal = usuario.IdSucursal;
        NombreSucursal = usuario.Sucursal?.Nombre ?? string.Empty;
    }

    public static void Limpiar()
    {
        IdUsuario = 0;
        NombreCompleto = string.Empty;
        Rol = string.Empty;
        IdSucursal = 0;
        NombreSucursal = string.Empty;
    }
}

