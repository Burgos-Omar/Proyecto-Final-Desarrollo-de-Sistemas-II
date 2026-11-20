using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FerreteriaERP.Core.Entities;

namespace FerreteriaERP.Core.Interfaces
{
    public interface IVentaRepository : IRepository<Venta>
    {
        /// <summary>
        /// Registra una venta completa en UNA sola transacción:
        /// recalcula subtotales y total, descuenta el stock de la sucursal, guarda la venta con sus detalles,
        /// y si el pago es a Crédito crea el registro de <see cref="Credito"/>.
        /// Si algo falla (ej. <see cref="Exceptions.StockInsuficienteException"/>) no se guarda nada.
        /// </summary>
        /// <param name="venta">Venta con <c>Detalles</c>, <c>IdUsuario</c>, <c>IdSucursal</c> y <c>MetodoPago</c> llenos.</param>
        /// <param name="fechaVencimientoCredito">Solo para pagos a crédito (fecha límite del cliente).</param>
        Task<Venta> RegistrarVentaAsync(Venta venta, DateTime? fechaVencimientoCredito = null, CancellationToken ct = default);

        /// <summary>Obtiene una venta con sus detalles, productos, cajero y cliente.</summary>
        Task<Venta?> ObtenerConDetallesAsync(int idVenta, CancellationToken ct = default);

        /// <summary>Ventas de una sucursal en el rango [desde, hasta) (fechas en UTC), más recientes primero.</summary>
        Task<IReadOnlyList<Venta>> ObtenerPorSucursalAsync(int idSucursal, DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default);

        /// <summary>Suma de ventas de una sucursal en el rango [desde, hasta) (fechas en UTC).</summary>
        Task<decimal> ObtenerTotalVendidoAsync(int idSucursal, DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default);
    }
}
