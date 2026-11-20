using System.Linq.Expressions;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FerreteriaERP.Infrastructure.Repositories
{
    /// <summary>
    /// Implementación base del repositorio genérico.
    /// Cada operación crea un <see cref="FerreteriaDbContext"/> de vida corta (patrón recomendado en apps de escritorio),
    /// evitando contextos eternos con datos desactualizados. Por eso los repositorios no guardan estado
    /// y pueden registrarse como Singleton.
    /// </summary>
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly IDbContextFactory<FerreteriaDbContext> ContextFactory;

        public Repository(IDbContextFactory<FerreteriaDbContext> contextFactory)
        {
            ContextFactory = contextFactory;
        }

        public virtual async Task<T?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            return await db.Set<T>().FindAsync(new object[] { id }, ct);
        }

        public virtual async Task<IReadOnlyList<T>> ObtenerTodosAsync(CancellationToken ct = default)
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            return await db.Set<T>().AsNoTracking().ToListAsync(ct);
        }

        public virtual async Task<IReadOnlyList<T>> BuscarAsync(
            Expression<Func<T, bool>> predicado, CancellationToken ct = default)
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            return await db.Set<T>().AsNoTracking().Where(predicado).ToListAsync(ct);
        }

        public virtual async Task<bool> ExisteAsync(
            Expression<Func<T, bool>> predicado, CancellationToken ct = default)
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            return await db.Set<T>().AnyAsync(predicado, ct);
        }

        public virtual async Task<int> ContarAsync(
            Expression<Func<T, bool>>? predicado = null, CancellationToken ct = default)
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            return predicado is null
                ? await db.Set<T>().CountAsync(ct)
                : await db.Set<T>().CountAsync(predicado, ct);
        }

        public virtual async Task<T> AgregarAsync(T entidad, CancellationToken ct = default)
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            db.Set<T>().Add(entidad);
            await db.SaveChangesAsync(ct); // Tras guardar, entidad.Id ya trae el valor generado.
            return entidad;
        }

        public virtual async Task ActualizarAsync(T entidad, CancellationToken ct = default)
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            // Se marca solo esta entidad como modificada; Update() recorrería todo el grafo
            // y podría intentar insertar/actualizar entidades relacionadas sin querer.
            db.Entry(entidad).State = EntityState.Modified;
            await db.SaveChangesAsync(ct);
        }

        public virtual async Task EliminarAsync(T entidad, CancellationToken ct = default)
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            db.Entry(entidad).State = EntityState.Deleted;
            await db.SaveChangesAsync(ct);
        }
    }
}
