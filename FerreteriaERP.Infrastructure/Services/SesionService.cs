using System;
using FerreteriaERP.Core.Entities;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Core.Services;

namespace FerreteriaERP.Infrastructure.Services
{
    public class SesionService : ISesionService
    {
        private readonly object _candado = new();
        private Usuario? _usuarioActual;

        public Usuario? UsuarioActual
        {
            get { lock (_candado) return _usuarioActual; }
        }

        public bool EstaAutenticado => UsuarioActual is not null;

        public int IdUsuarioActual =>
            UsuarioActual?.Id ?? throw new InvalidOperationException("No hay ningún usuario autenticado en la sesión.");

        public int IdSucursalActual =>
            UsuarioActual?.IdSucursal ?? throw new InvalidOperationException("No hay ninguna sucursal activa en la sesión.");

        public void EstablecerSesion(Usuario usuario)
        {
            ArgumentNullException.ThrowIfNull(usuario);
            lock (_candado)
            {
                _usuarioActual = usuario;
                SesionActual.Establecer(usuario);
            }
        }

        public void CerrarSesion()
        {
            lock (_candado)
            {
                _usuarioActual = null;
                SesionActual.Limpiar();
            }
        }
    }
}

