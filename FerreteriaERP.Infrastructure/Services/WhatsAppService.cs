using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using FerreteriaERP.Core.Interfaces;

namespace FerreteriaERP.Infrastructure.Services
{
    /// <summary>
    /// Implementación del servicio de integración con WhatsApp mediante enlaces profundos <c>https://wa.me/</c>.
    /// </summary>
    public class WhatsAppService : IWhatsAppService
    {
        /// <inheritdoc />
        public void EnviarRecordatorioPago(string celular, string nombreCliente, decimal saldo, DateTime vencimiento)
        {
            var celularLimpio = LimpiarNumeroCelular(celular);

            var nombreMostrado = string.IsNullOrWhiteSpace(nombreCliente)
                ? "Estimado(a) cliente"
                : nombreCliente.Trim();

            var fechaLocal = vencimiento.Kind == DateTimeKind.Utc
                ? vencimiento.ToLocalTime()
                : vencimiento;

            var saldoFormateado = saldo.ToString("N2", CultureInfo.InvariantCulture);
            var fechaFormateada = fechaLocal.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

            var mensaje =
                $"Hola {nombreMostrado}, le saludamos cordialmente de Ferretería ERP. " +
                $"Le recordamos amablemente que registra un saldo pendiente de Bs {saldoFormateado} " +
                $"con fecha de vencimiento el {fechaFormateada}. " +
                $"Quedamos atentos a su pago o consulta. ¡Muchas gracias por su preferencia!";

            AbrirEnlaceWhatsApp(celularLimpio, mensaje);
        }

        /// <inheritdoc />
        public void EnviarSolicitudReabastecimiento(
            string celular,
            string nombreProveedor,
            string nombreProducto,
            string? codigoProducto)
        {
            var celularLimpio = LimpiarNumeroCelular(celular);

            var proveedorMostrado = string.IsNullOrWhiteSpace(nombreProveedor)
                ? "Proveedor"
                : nombreProveedor.Trim();

            var productoMostrado = string.IsNullOrWhiteSpace(nombreProducto)
                ? "Producto"
                : nombreProducto.Trim();

            var codigoMostrado = string.IsNullOrWhiteSpace(codigoProducto)
                ? "S/C"
                : codigoProducto.Trim();

            var mensaje =
                $"Hola {proveedorMostrado}, nos comunicamos de la Ferretería. " +
                $"Necesitamos reabastecer el producto {productoMostrado} (Código: {codigoMostrado}). " +
                $"Nuestro stock actual está bajo. ¿Podría confirmarnos disponibilidad y tiempos de entrega?";

            AbrirEnlaceWhatsApp(celularLimpio, mensaje);
        }

        private static string LimpiarNumeroCelular(string celular)
        {
            if (string.IsNullOrWhiteSpace(celular))
                throw new ArgumentException("No hay un número de celular/WhatsApp registrado.", nameof(celular));

            var celularLimpio = new string(celular.Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(celularLimpio))
                throw new ArgumentException("El número de celular de WhatsApp no contiene dígitos válidos.", nameof(celular));

            return celularLimpio;
        }

        private static void AbrirEnlaceWhatsApp(string celularLimpio, string mensaje)
        {
            var mensajeFormateado = Uri.EscapeDataString(mensaje);
            var url = $"https://wa.me/{celularLimpio}?text={mensajeFormateado}";

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
}
