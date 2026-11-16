using System;

namespace FerreteriaERP.Core.Services;

/// <summary>
/// Convierte montos numéricos decimales a su representación literal en español para recibos y notas de venta en Bolivia.
/// Ejemplo: 150.50 -> "CIENTO CINCUENTA 50/100 BOLIVIANOS".
/// </summary>
public static class NumeroALetras
{
    private static readonly string[] Unidades =
    {
        "", "UN", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE",
        "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE", "DIECISEIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE",
        "VEINTE", "VEINTIUN", "VEINTIDOS", "VEINTITRES", "VEINTICUATRO", "VEINTICINCO", "VEINTISEIS", "VEINTISIETE", "VEINTIOCHO", "VEINTINUEVE"
    };

    private static readonly string[] Decenas =
    {
        "", "DIEZ", "VEINTE", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA"
    };

    private static readonly string[] Centenas =
    {
        "", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS",
        "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS"
    };

    public static string Convertir(decimal numero, string moneda = "BOLIVIANOS")
    {
        if (numero < 0m)
            numero = Math.Abs(numero);

        var redondeado = Math.Round(numero, 2, MidpointRounding.AwayFromZero);
        var parteEntera = (long)Math.Truncate(redondeado);
        var centavos = (int)Math.Round((redondeado - parteEntera) * 100m);

        var letrasEntero = parteEntera == 0
            ? "CERO"
            : ConvertirEntero(parteEntera).Trim();

        var sufijoMoneda = string.IsNullOrWhiteSpace(moneda)
            ? string.Empty
            : $" {moneda.Trim().ToUpperInvariant()}";

        return $"{letrasEntero} {centavos:00}/100{sufijoMoneda}";
    }

    private static string ConvertirEntero(long numero)
    {
        if (numero == 0)
            return string.Empty;

        if (numero == 100)
            return "CIEN";

        if (numero < 30)
            return Unidades[numero];

        if (numero < 100)
        {
            var decena = numero / 10;
            var unidad = numero % 10;
            return unidad == 0
                ? Decenas[decena]
                : $"{Decenas[decena]} Y {Unidades[unidad]}";
        }

        if (numero < 1000)
        {
            var centena = numero / 100;
            var resto = numero % 100;
            return resto == 0
                ? Centenas[centena]
                : $"{Centenas[centena]} {ConvertirEntero(resto)}";
        }

        if (numero < 1_000_000)
        {
            var miles = numero / 1000;
            var resto = numero % 1000;

            var textoMiles = miles == 1
                ? "MIL"
                : $"{ConvertirEntero(miles)} MIL";

            return resto == 0
                ? textoMiles
                : $"{textoMiles} {ConvertirEntero(resto)}";
        }

        var millones = numero / 1_000_000;
        var restoMillones = numero % 1_000_000;

        var textoMillones = millones == 1
            ? "UN MILLON"
            : $"{ConvertirEntero(millones)} MILLONES";

        return restoMillones == 0
            ? textoMillones
            : $"{textoMillones} {ConvertirEntero(restoMillones)}";
    }
}

