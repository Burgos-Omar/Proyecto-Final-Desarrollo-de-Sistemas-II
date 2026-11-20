using System;

namespace FerreteriaERP.Core.Interfaces
{
    /// <summary>
    /// Servicio para el envío de notificaciones de cobro y pedidos de reabastecimiento a través de WhatsApp (wa.me).
    /// </summary>
    public interface IWhatsAppService
    {
        /// <summary>
        /// Formatea un mensaje amable de recordatorio de pago (URL encoded) y abre en el navegador
        /// predeterminado el enlace <c>https://wa.me/{celular}?text={mensaje_formateado}</c>.
        /// </summary>
        void EnviarRecordatorioPago(string celular, string nombreCliente, decimal saldo, DateTime vencimiento);

        /// <summary>
        /// Formatea una solicitud de reabastecimiento de inventario para un proveedor y abre el enlace
        /// <c>https://wa.me/{celular}?text={mensaje_formateado}</c>.
        /// </summary>
        void EnviarSolicitudReabastecimiento(
            string celular,
            string nombreProveedor,
            string nombreProducto,
            string? codigoProducto);
    }
}
