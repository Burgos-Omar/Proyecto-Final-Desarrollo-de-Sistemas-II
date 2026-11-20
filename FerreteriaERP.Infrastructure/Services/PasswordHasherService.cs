using BCrypt.Net;
using FerreteriaERP.Core.Interfaces;

namespace FerreteriaERP.Infrastructure.Services
{
    /// <summary>
    /// Implementación de hashing de contraseñas mediante BCrypt.Net-Next.
    /// </summary>
    public class PasswordHasherService : IPasswordHasherService
    {
        private const int WorkFactor = 12;

        public string HashPassword(string password)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(password);
            return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(passwordHash))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch (SaltParseException)
            {
                // Si el hash almacenado tiene un formato inválido o corrupto, se rechaza sin romper el flujo.
                return false;
            }
        }
    }
}

