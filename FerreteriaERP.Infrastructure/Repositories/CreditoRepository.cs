using FerreteriaERP.Core.Constants;
using FerreteriaERP.Core.Entities;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FerreteriaERP.Infrastructure.Repositories
{
    public class CreditoRepository : Repository<Credito>, ICreditoRepository
    {
        public CreditoRepository(IDbContextFactory<FerreteriaDbContext> contextFactory) : base(contextFactory)
        {
        }

        public async Task<IReadOnlyList<Credito>> ObtenerPendientesPorClienteAsync(int idCliente, CancellationToken ct = default)
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            return await db.Creditos
                .AsNoTracking()
                .Include(c => c.Abonos)
                .Where(c => c.IdCliente == idCliente && c.Estado == EstadosCredito.Pendiente)
                .OrderBy(c => c.Id)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Credito>> ObtenerVencidosAsync(DateTime ahoraUtc, CancellationToken ct = default)
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            return await db.Creditos
                .AsNoTracking()
                .Include(c => c.Cliente)
                .Where(c => c.Estado == EstadosCredito.Pendiente
                            && c.FechaVencimiento != null && c.FechaVencimiento < ahoraUtc)
                .OrderBy(c => c.FechaVencimiento)
                .ToListAsync(ct);
        }

        public async Task<AbonoCredito> RegistrarAbonoAsync(int idCredito, decimal monto, CancellationToken ct = default)
        {
            if (monto <= 0)
                throw new InvalidOperationException("El monto del abono debe ser mayor a cero.");

            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            await using var transaccion = await db.Database.BeginTransactionAsync(ct);

            // Descuento atómico: solo procede si el crédito está pendiente y el saldo alcanza.
            var filas = await db.Creditos
                .Where(c => c.Id == idCredito && c.Estado == EstadosCredito.Pendiente && c.SaldoPendiente >= monto)
                .ExecuteUpdateAsync(c => c.SetProperty(x => x.SaldoPendiente, x => x.SaldoPendiente - monto), ct);

            if (filas == 0)
                throw new InvalidOperationException("El crédito no existe, ya está pagado o el abono supera el saldo pendiente.");

            var abono = new AbonoCredito
            {
                IdCredito = idCredito,
                MontoPagado = monto,
                FechaPago = DateTime.UtcNow
            };
            db.AbonosCredito.Add(abono);
            await db.SaveChangesAsync(ct);

            // Si el saldo llegó a cero, el crédito queda como Pagado.
            await db.Creditos
                .Where(c => c.Id == idCredito && c.SaldoPendiente == 0)
                .ExecuteUpdateAsync(c => c.SetProperty(x => x.Estado, EstadosCredito.Pagado), ct);

            await transaccion.CommitAsync(ct);
            return abono;
        }
    }
}
