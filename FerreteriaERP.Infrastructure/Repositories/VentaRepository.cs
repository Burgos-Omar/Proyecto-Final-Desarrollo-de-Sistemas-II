using FerreteriaERP.Core.Constants;
using FerreteriaERP.Core.Entities;
using FerreteriaERP.Core.Exceptions;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FerreteriaERP.Infrastructure.Repositories
{
    public class VentaRepository : Repository<Venta>, IVentaRepository
    {
        public VentaRepository(IDbContextFactory<FerreteriaDbContext> contextFactory) : base(contextFactory)
        {
        }

        public async Task<Venta> RegistrarVentaAsync(
            Venta venta, DateTime? fechaVencimientoCredito = null, CancellationToken ct = default)
        {
            // ---- Validaciones de negocio ----
            if (venta.Detalles.Count == 0)
                throw new InvalidOperationException("La venta no tiene productos.");

            if (venta.MetodoPago == MetodosPago.Credito && venta.IdCliente is null)
                throw new InvalidOperationException("Una venta a crédito requiere un cliente confirmado.");

            // ---- Cálculo autoritativo: no se confía en los montos que manda la UI ----
            foreach (var detalle in venta.Detalles)
                detalle.Subtotal = detalle.Cantidad * detalle.PrecioUnitario;

            venta.Total = venta.Detalles.Sum(d => d.Subtotal);
            venta.FechaHora = DateTime.UtcNow; // Npgsql exige UTC para "timestamp with time zone".

            switch (venta.MetodoPago)
            {
                case MetodosPago.Efectivo:
                    if (venta.MontoRecibido < venta.Total)
                        throw new InvalidOperationException("El monto recibido es menor al total de la venta.");
                    venta.CambioDado = venta.MontoRecibido - venta.Total;
                    break;
                case MetodosPago.QR:
                    venta.MontoRecibido = venta.Total;
                    venta.CambioDado = 0;
                    break;
                case MetodosPago.Credito:
                    venta.MontoRecibido = 0; // Se cobra después mediante abonos.
                    venta.CambioDado = 0;
                    break;
                default:
                    throw new InvalidOperationException($"Método de pago no válido: {venta.MetodoPago}");
            }

            // ---- Todo o nada: stock + venta + crédito en una sola transacción ----
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            await using var transaccion = await db.Database.BeginTransactionAsync(ct);

            // Se agrupa por producto y se ordena por Id para evitar bloqueos cruzados (deadlocks).
            var cantidadesPorProducto = venta.Detalles
                .GroupBy(d => d.IdProducto)
                .OrderBy(g => g.Key)
                .Select(g => new { IdProducto = g.Key, Cantidad = g.Sum(d => d.Cantidad) });

            foreach (var item in cantidadesPorProducto)
            {
                var cantidad = item.Cantidad;
                var idProducto = item.IdProducto;

                // Descuento atómico: solo se aplica si alcanza el stock de ESTA sucursal.
                var filas = await db.StockSucursales
                    .Where(s => s.IdProducto == idProducto && s.IdSucursal == venta.IdSucursal
                                && s.CantidadActual >= cantidad)
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(x => x.CantidadActual, x => x.CantidadActual - cantidad), ct);

                if (filas == 0)
                    throw new StockInsuficienteException(idProducto, venta.IdSucursal, cantidad);
            }

            db.Ventas.Add(venta);
            await db.SaveChangesAsync(ct); // Genera venta.Id

            if (venta.IdCliente is int idCliente)
            {
                if (venta.MetodoPago == MetodosPago.Credito)
                {
                    var vencimientoUtc = fechaVencimientoCredito?.ToUniversalTime()
                                         ?? DateTime.UtcNow.AddDays(30);

                    db.Creditos.Add(new Credito
                    {
                        IdVenta = venta.Id,
                        IdCliente = idCliente,
                        MontoTotalDeuda = venta.Total,
                        SaldoPendiente = venta.Total,
                        FechaVencimiento = vencimientoUtc,
                        Estado = EstadosCredito.Pendiente
                    });
                    await db.SaveChangesAsync(ct);
                }

                await db.Clientes
                    .Where(c => c.Id == idCliente)
                    .ExecuteUpdateAsync(c => c.SetProperty(x => x.CantidadCompras, x => x.CantidadCompras + 1), ct);
            }

            await transaccion.CommitAsync(ct);
            return venta;
        }

        public async Task<Venta?> ObtenerConDetallesAsync(int idVenta, CancellationToken ct = default)
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            return await db.Ventas
                .AsNoTracking()
                .Include(v => v.Detalles).ThenInclude(d => d.Producto)
                .Include(v => v.Usuario)
                .Include(v => v.Cliente)
                .FirstOrDefaultAsync(v => v.Id == idVenta, ct);
        }

        public async Task<IReadOnlyList<Venta>> ObtenerPorSucursalAsync(
            int idSucursal, DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default)
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            return await db.Ventas
                .AsNoTracking()
                .Include(v => v.Usuario)
                .Where(v => v.IdSucursal == idSucursal && v.FechaHora >= desdeUtc && v.FechaHora < hastaUtc)
                .OrderByDescending(v => v.FechaHora)
                .ToListAsync(ct);
        }

        public async Task<decimal> ObtenerTotalVendidoAsync(
            int idSucursal, DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default)
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            return await db.Ventas
                .Where(v => v.IdSucursal == idSucursal && v.FechaHora >= desdeUtc && v.FechaHora < hastaUtc)
                .SumAsync(v => v.Total, ct);
        }
    }
}
