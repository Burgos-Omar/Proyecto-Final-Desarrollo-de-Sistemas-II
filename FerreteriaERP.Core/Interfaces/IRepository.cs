using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace FerreteriaERP.Core.Interfaces
{
    /// <summary>
    /// Repositorio base genérico con las operaciones CRUD comunes a todas las entidades.
    /// Los repositorios específicos (IProductoRepository, etc.) heredan de esta interfaz
    /// y agregan sus consultas propias.
    /// </summary>
    /// <typeparam name="T">Entidad del dominio (todas usan <c>int Id</c> como clave primaria).</typeparam>
    public interface IRepository<T> where T : class
    {
        /// <summary>Busca por clave primaria. Devuelve null si no existe.</summary>
        Task<T?> ObtenerPorIdAsync(int id, CancellationToken ct = default);

        /// <summary>Devuelve todos los registros (usar con cuidado en tablas grandes).</summary>
        Task<IReadOnlyList<T>> ObtenerTodosAsync(CancellationToken ct = default);

        /// <summary>Devuelve los registros que cumplan el predicado.</summary>
        Task<IReadOnlyList<T>> BuscarAsync(Expression<Func<T, bool>> predicado, CancellationToken ct = default);

        /// <summary>Indica si existe al menos un registro que cumpla el predicado.</summary>
        Task<bool> ExisteAsync(Expression<Func<T, bool>> predicado, CancellationToken ct = default);

        /// <summary>Cuenta los registros (todos, o solo los que cumplan el predicado).</summary>
        Task<int> ContarAsync(Expression<Func<T, bool>>? predicado = null, CancellationToken ct = default);

        /// <summary>Inserta la entidad y devuelve la misma instancia con su Id generado.</summary>
        Task<T> AgregarAsync(T entidad, CancellationToken ct = default);

        /// <summary>Actualiza solo la entidad indicada (no sus entidades relacionadas).</summary>
        Task ActualizarAsync(T entidad, CancellationToken ct = default);

        /// <summary>Elimina la entidad indicada.</summary>
        Task EliminarAsync(T entidad, CancellationToken ct = default);
    }
}
