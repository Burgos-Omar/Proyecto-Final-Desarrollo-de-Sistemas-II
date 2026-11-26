using System;
using BCrypt.Net;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Core.Security;

namespace FerreteriaERP.Infrastructure.Services
{
    /// <summary>
    /// Implementación de hashing de contraseñas mediante PBKDF2-SHA256 con salt aleatorio (PASO 32),
    /// manteniendo compatibilidad de verificación con hashes BCrypt heredados para evitar bloqueos de cuentas.
    /// </summary>
    public class PasswordHasherService : IPasswordHasherService
    {
        public string HashPassword(string password)
        {
            return PasswordHasher.HashPassword(password);
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(passwordHash))
                return false;

            if (PasswordHasher.IsHashed(passwordHash))
                return PasswordHasher.VerifyPassword(password, passwordHash);

            // Compatibilidad temporal con hashes BCrypt existentes antes de la migración a PBKDF2
            if (passwordHash.StartsWith("$2", StringComparison.Ordinal))
            {
                try
                {
                    return BCrypt.Net.BCrypt.Verify(password, passwordHash);
                }
                catch (SaltParseException)
                {
                    return false;
                }
            }

            return false;
        }
    }
}

