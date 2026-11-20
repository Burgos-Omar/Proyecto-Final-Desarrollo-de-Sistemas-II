using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FerreteriaERP.Core.Entities;

namespace FerreteriaERP.Core.Interfaces
{
    public interface ICreditoRepository : IRepository<Credito>
    {
        /// <summary>Créditos con saldo pendiente de un cliente, más antiguos primero.</summary>
        Task<IReadOnlyList<Credito>> ObtenerPendientesPorClienteAsync(int idCliente, CancellationToken ct = default);

        /// <summary>Créditos pendientes cuya fecha de vencimiento ya pasó (fecha en UTC).</summary>
        Task<IReadOnlyList<Credito>> ObtenerVencidosAsync(DateTime ahoraUtc, CancellationToken ct = default);

        /// <summary>
        /// Registra un abono de forma atómica: descuenta el saldo, guarda el <see cref="AbonoCredito"/>
        /// y marca el crédito como "Pagado" cuando el saldo llega a cero.
        /// Lanza <see cref="InvalidOperationException"/> si el monto es inválido o supera el saldo.
        /// </summary>
        Task<AbonoCredito> RegistrarAbonoAsync(int idCredito, decimal monto, CancellationToken ct = default);
    }
}
