using System;

namespace FerreteriaERP.Core.Exceptions
{
    /// <summary>
    /// Se lanza cuando se intenta vender (o descontar) más unidades de las que hay
    /// disponibles en el stock de una sucursal.
    /// </summary>
    public class StockInsuficienteException : Exception
    {
        public int IdProducto { get; }
        public int IdSucursal { get; }
        public int CantidadSolicitada { get; }

        public StockInsuficienteException(int idProducto, int idSucursal, int cantidadSolicitada)
            : base($"Stock insuficiente para el producto {idProducto} en la sucursal {idSucursal} " +
                   $"(se solicitaron {cantidadSolicitada} unidades).")
        {
            IdProducto = idProducto;
            IdSucursal = idSucursal;
            CantidadSolicitada = cantidadSolicitada;
        }
    }
}
