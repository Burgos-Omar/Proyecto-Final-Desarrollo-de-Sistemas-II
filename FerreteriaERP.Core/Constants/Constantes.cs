namespace FerreteriaERP.Core.Constants
{
    // Las entidades guardan estos valores como texto (string) en la base de datos.
    // Centralizarlos aquí evita "strings mágicos" repartidos por el código.

    /// <summary>Valores válidos de <c>Usuario.Estado</c>.</summary>
    public static class EstadosUsuario
    {
        public const string Activo = "Activo";
        public const string Inhabilitado = "Inhabilitado";
    }

    /// <summary>Valores válidos de <c>Venta.MetodoPago</c>.</summary>
    public static class MetodosPago
    {
        public const string Efectivo = "Efectivo";
        public const string QR = "QR";
        public const string Credito = "Credito";
    }

    /// <summary>Valores válidos de <c>Credito.Estado</c>.</summary>
    public static class EstadosCredito
    {
        public const string Pendiente = "Pendiente";
        public const string Pagado = "Pagado";
    }

    /// <summary>Valores válidos de <c>Entrega.EstadoEntrega</c>.</summary>
    public static class EstadosEntrega
    {
        public const string EnPreparacion = "En Preparacion";
        public const string EnRuta = "En Ruta";
        public const string Entregado = "Entregado";
    }
}
