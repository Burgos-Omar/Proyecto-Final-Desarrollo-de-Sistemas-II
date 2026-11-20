using FerreteriaERP.Core.Entities;

namespace FerreteriaERP.Core.Interfaces
{
    /// <summary>
    /// Mantiene el contexto del usuario y sucursal autenticados durante la sesión activa.
    /// </summary>
    public interface ISesionService
    {
        Usuario? UsuarioActual { get; }

        bool EstaAutenticado { get; }

        int IdUsuarioActual { get; }

        int IdSucursalActual { get; }

        void EstablecerSesion(Usuario usuario);

        void CerrarSesion();
    }
}

