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
    /// PASO 33 (RBAC): Indica si el usuario autenticado posee acceso administrativo completo
    /// (SuperAdmin, Administrador, Admin o Administrativo).
    /// </summary>
    public static bool TieneAccesoAdministrativo =>
        string.Equals(Rol?.Trim(), "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Rol?.Trim(), "Administrador", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Rol?.Trim(), "Admin", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Rol?.Trim(), "Administrativo", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Indica si el usuario autenticado posee rol de Administrador.
    /// </summary>
    public static bool EsAdmin => TieneAccesoAdministrativo;

    /// <summary>
    /// Indica si el usuario autenticado es Dueño / SuperAdmin o Administrador con acceso a Inteligencia Gerencial.
    /// </summary>
    public static bool EsSuperAdmin => TieneAccesoAdministrativo;

    /// <summary>
    /// Indica si el usuario puede acceder al módulo de Reabastecimiento y Órdenes de Compra (Admin / SuperAdmin / Administrativo / Almacenero).
    /// </summary>
    public static bool PuedeGestionarReabastecimiento =>
        TieneAccesoAdministrativo ||
        string.Equals(Rol?.Trim(), "Almacenero", StringComparison.OrdinalIgnoreCase);

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

