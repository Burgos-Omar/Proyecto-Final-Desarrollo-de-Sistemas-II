using System;
using System.Security.Cryptography;

namespace FerreteriaERP.Core.Security;

/// <summary>
/// Servicio criptográfico para el hashing seguro de contraseñas mediante PBKDF2-SHA256 con salt aleatorio (PASO 32).
/// Formato de almacenamiento: "PBKDF2$100000$&lt;saltBase64&gt;$&lt;hashBase64&gt;".
/// </summary>
public static class PasswordHasher
{
    private const string PrefijoAlgoritmo = "PBKDF2";
    private const int IteracionesPorDefecto = 100_000;
    private const int TamanoSaltBytes = 16;
    private const int TamanoHashBytes = 32;

    /// <summary>
    /// Genera un hash irreversible PBKDF2-SHA256 con salt criptográfico aleatorio de 16 bytes y 100,000 iteraciones.
    /// </summary>
    public static string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        byte[] salt = RandomNumberGenerator.GetBytes(TamanoSaltBytes);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            IteracionesPorDefecto,
            HashAlgorithmName.SHA256,
            TamanoHashBytes);

        return $"{PrefijoAlgoritmo}${IteracionesPorDefecto}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    /// <summary>
    /// Verifica si una contraseña en texto plano coincide con un hash almacenado usando comparación en tiempo constante.
    /// </summary>
    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || !IsHashed(storedHash))
            return false;

        var partes = storedHash.Split('$');
        if (partes.Length != 4)
            return false;

        if (!string.Equals(partes[0], PrefijoAlgoritmo, StringComparison.Ordinal))
            return false;

        if (!int.TryParse(partes[1], out int iteraciones) || iteraciones <= 0)
            return false;

        byte[] salt;
        byte[] hashEsperado;
        try
        {
            salt = Convert.FromBase64String(partes[2]);
            hashEsperado = Convert.FromBase64String(partes[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (salt.Length == 0 || hashEsperado.Length == 0)
            return false;

        byte[] hashCalculado = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iteraciones,
            HashAlgorithmName.SHA256,
            hashEsperado.Length);

        return CryptographicOperations.FixedTimeEquals(hashCalculado, hashEsperado);
    }

    /// <summary>
    /// Indica si el texto proporcionado ya posee el formato PBKDF2-SHA256 generado por este servicio.
    /// </summary>
    public static bool IsHashed(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.StartsWith($"{PrefijoAlgoritmo}$", StringComparison.Ordinal);
    }
}
