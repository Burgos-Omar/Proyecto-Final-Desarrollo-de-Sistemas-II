namespace FerreteriaERP.Core.Interfaces
{
    /// <summary>
    /// Servicio para el hashing irreversible y verificación de contraseñas de usuarios.
    /// </summary>
    public interface IPasswordHasherService
    {
        /// <summary>Genera un hash seguro (BCrypt) a partir de una contraseña en texto plano.</summary>
        string HashPassword(string password);

        /// <summary>Verifica si una contraseña en texto plano coincide con el hash almacenado.</summary>
        bool VerifyPassword(string password, string passwordHash);
    }
}

