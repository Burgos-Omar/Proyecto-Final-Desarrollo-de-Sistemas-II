using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FerreteriaERP.Core.Constants;
using FerreteriaERP.Core.Entities;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Core.Security;
using FerreteriaERP.Core.Services;
using FerreteriaERP.Infrastructure.Data;
using FerreteriaERP.Infrastructure.Repositories;
using FerreteriaERP.Infrastructure.Services;
using FerreteriaERP.UI.Desktop.ViewModels;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FerreteriaERP.Tests;

[CollectionDefinition("SesionActualTests", DisableParallelization = true)]
public class SesionActualCollectionDefinition
{
}

/// <summary>
/// Pruebas unitarias del Módulo de Inicio de Sesión (Login) y Control de Sesión (SesionActual)
/// siguiendo estrictamente el patrón AAA (Arrange, Act, Assert) con EF Core InMemory.
/// </summary>
[Collection("SesionActualTests")]
public class LoginTests : IDisposable
{
    public LoginTests()
    {
        // Garantiza un estado inicial limpio antes de cada prueba
        SesionActual.Limpiar();
    }

    public void Dispose()
    {
        // Garantiza que ninguna prueba contamine a la siguiente
        SesionActual.Limpiar();
    }

    [Fact]
    public async Task Login_ConCredencialesValidas_DeberiaAutenticarYEstablecerSesion()
    {
        // Arrange
        SesionActual.Limpiar();
        var dbFactory = CrearInMemoryFactory();
        var passwordHasher = new PasswordHasherService();
        const string passwordPlana = "Ferreteria2026*";

        await using (var dbSeed = await dbFactory.CreateDbContextAsync())
        {
            var rolAdmin = new Rol { Id = 1, Nombre = "Administrador" };
            var sucursalCentral = new Sucursal { Id = 1, Nombre = "Sucursal Central", EsCentral = true };

            var usuarioActivo = new Usuario
            {
                Id = 10,
                Ci = "jvargas",
                NombreCompleto = "Juan Vargas",
                PasswordHash = PasswordHasher.HashPassword(passwordPlana),
                Estado = EstadosUsuario.Activo,
                FechaInicio = DateTime.UtcNow,
                Rol = rolAdmin,
                Sucursal = sucursalCentral
            };

            dbSeed.Usuarios.Add(usuarioActivo);
            await dbSeed.SaveChangesAsync();
        }

        var usuarioRepo = new UsuarioRepository(dbFactory);
        var loginVm = new LoginViewModel(usuarioRepo, passwordHasher)
        {
            Usuario = "jvargas",
            Password = passwordPlana
        };

        Usuario? usuarioAutenticadoEvento = null;
        loginVm.SesionIniciada += u => usuarioAutenticadoEvento = u;

        // Act
        await loginVm.IniciarSesionCommand.ExecuteAsync(null);

        // Assert
        Assert.False(loginVm.TieneError);
        Assert.Null(loginVm.MensajeError);
        Assert.NotNull(usuarioAutenticadoEvento);
        Assert.True(SesionActual.EstaAutenticado);
        Assert.Equal(10, SesionActual.IdUsuario);
        Assert.Equal("Juan Vargas", SesionActual.NombreCompleto);
        Assert.Equal("Administrador", SesionActual.Rol);
        Assert.Equal(1, SesionActual.IdSucursal);
        Assert.Equal("Sucursal Central", SesionActual.NombreSucursal);
        Assert.True(SesionActual.EsAdmin);

        SesionActual.Limpiar();
    }

    [Fact]
    public async Task Login_ConContrasenaIncorrecta_DeberiaRechazarAcceso()
    {
        // Arrange
        SesionActual.Limpiar();
        var dbFactory = CrearInMemoryFactory();
        var passwordHasher = new PasswordHasherService();

        await using (var dbSeed = await dbFactory.CreateDbContextAsync())
        {
            var rolCajero = new Rol { Id = 2, Nombre = "Cajero" };
            var sucursalNorte = new Sucursal { Id = 2, Nombre = "Sucursal Norte", EsCentral = false };

            dbSeed.Usuarios.Add(new Usuario
            {
                Id = 20,
                Ci = "mrojas",
                NombreCompleto = "María Rojas",
                PasswordHash = PasswordHasher.HashPassword("ClaveCorrecta123"),
                Estado = EstadosUsuario.Activo,
                FechaInicio = DateTime.UtcNow,
                Rol = rolCajero,
                Sucursal = sucursalNorte
            });
            await dbSeed.SaveChangesAsync();
        }

        var usuarioRepo = new UsuarioRepository(dbFactory);
        var loginVm = new LoginViewModel(usuarioRepo, passwordHasher)
        {
            Usuario = "mrojas",
            Password = "ClaveErronea999"
        };

        var eventoDisparado = false;
        loginVm.SesionIniciada += _ => eventoDisparado = true;

        // Act
        await loginVm.IniciarSesionCommand.ExecuteAsync(null);

        // Assert
        Assert.False(eventoDisparado);
        Assert.True(loginVm.TieneError);
        Assert.Equal("Usuario o contraseña incorrectos.", loginVm.MensajeError);
        Assert.False(SesionActual.EstaAutenticado);
        Assert.Equal(0, SesionActual.IdUsuario);
        Assert.Equal(string.Empty, SesionActual.NombreCompleto);

        SesionActual.Limpiar();
    }

    [Fact]
    public async Task Login_ConUsuarioInactivoSoftDelete_DeberiaDenegarAcceso()
    {
        // Arrange
        SesionActual.Limpiar();
        var dbFactory = CrearInMemoryFactory();
        var passwordHasher = new PasswordHasherService();
        const string passwordCorrecta = "Vendedor2026";

        await using (var dbSeed = await dbFactory.CreateDbContextAsync())
        {
            var rolVendedor = new Rol { Id = 3, Nombre = "Vendedor" };
            var sucursalSur = new Sucursal { Id = 3, Nombre = "Sucursal Sur", EsCentral = false };

            // Empleado dado de baja (EstaActivo = false) y su Usuario inhabilitado (Soft-Delete)
            var empleadoInactivo = new Empleado
            {
                Id = 30,
                Nombre = "Carlos",
                Apellidos = "Mendoza",
                Ci = "7120458",
                Puesto = "Vendedor",
                IdSucursal = sucursalSur.Id,
                Sucursal = sucursalSur,
                EstaActivo = false
            };

            var usuarioInhabilitado = new Usuario
            {
                Id = 30,
                Ci = "cmendoza",
                NombreCompleto = empleadoInactivo.NombreCompleto,
                PasswordHash = PasswordHasher.HashPassword(passwordCorrecta),
                Estado = EstadosUsuario.Inhabilitado,
                FechaInicio = DateTime.UtcNow,
                Rol = rolVendedor,
                Sucursal = sucursalSur
            };

            dbSeed.Empleados.Add(empleadoInactivo);
            dbSeed.Usuarios.Add(usuarioInhabilitado);
            await dbSeed.SaveChangesAsync();
        }

        var usuarioRepo = new UsuarioRepository(dbFactory);
        var loginVm = new LoginViewModel(usuarioRepo, passwordHasher)
        {
            Usuario = "cmendoza",
            Password = passwordCorrecta
        };

        var eventoDisparado = false;
        loginVm.SesionIniciada += _ => eventoDisparado = true;

        // Act
        await loginVm.IniciarSesionCommand.ExecuteAsync(null);

        // Assert
        Assert.False(eventoDisparado);
        Assert.True(loginVm.TieneError);
        Assert.Equal("Su cuenta se encuentra inhabilitada. Contacte al administrador.", loginVm.MensajeError);
        Assert.False(SesionActual.EstaAutenticado);
        Assert.Equal(0, SesionActual.IdUsuario);

        SesionActual.Limpiar();
    }

    [Fact]
    public async Task Login_ConCamposVacios_DeberiaValidarSinConsultarBD()
    {
        // Arrange
        SesionActual.Limpiar();
        var repoEspia = new UsuarioRepositoryEspia();
        var passwordHasher = new PasswordHasherService();

        var loginVm = new LoginViewModel(repoEspia, passwordHasher)
        {
            Usuario = "   ",
            Password = string.Empty
        };

        var eventoDisparado = false;
        loginVm.SesionIniciada += _ => eventoDisparado = true;

        // Act
        await loginVm.IniciarSesionCommand.ExecuteAsync(null);

        // Assert
        Assert.False(eventoDisparado);
        Assert.Equal(0, repoEspia.LlamadasObtenerPorCi);
        Assert.True(loginVm.TieneError);
        Assert.False(string.IsNullOrWhiteSpace(loginVm.MensajeError));
        Assert.False(SesionActual.EstaAutenticado);
        Assert.Equal(0, SesionActual.IdUsuario);

        SesionActual.Limpiar();
    }

    [Fact]
    public void PasswordHasher_MismaClave_DeberiaGenerarHashesDistintosPorSalt()
    {
        // Arrange
        const string clave = "SuperAdmin2026!";

        // Act
        var hash1 = PasswordHasher.HashPassword(clave);
        var hash2 = PasswordHasher.HashPassword(clave);

        // Assert
        Assert.NotEqual(hash1, hash2);
        Assert.True(PasswordHasher.IsHashed(hash1));
        Assert.True(PasswordHasher.IsHashed(hash2));
        Assert.StartsWith("PBKDF2$100000$", hash1);
        Assert.True(PasswordHasher.VerifyPassword(clave, hash1));
        Assert.True(PasswordHasher.VerifyPassword(clave, hash2));
    }

    [Fact]
    public void PasswordHasher_ClaveIncorrecta_DeberiaRechazarVerificacion()
    {
        // Arrange
        const string claveCorrecta = "FerreteriaSegura#99";
        var hashAlmacenado = PasswordHasher.HashPassword(claveCorrecta);

        // Act
        var resultadoErroneo = PasswordHasher.VerifyPassword("FerreteriaSegura#98", hashAlmacenado);
        var resultadoVacio = PasswordHasher.VerifyPassword(string.Empty, hashAlmacenado);
        var resultadoFormatoInvalido = PasswordHasher.VerifyPassword(claveCorrecta, "texto_plano_sin_hash");

        // Assert
        Assert.False(resultadoErroneo);
        Assert.False(resultadoVacio);
        Assert.False(resultadoFormatoInvalido);
        Assert.False(PasswordHasher.IsHashed("texto_plano_sin_hash"));
    }

    [Fact]
    public async Task DbSeeder_UsuariosConContrasenaTextoPlano_DeberiaMigrarAutomaticamenteAPbkdf2()
    {
        // Arrange
        var dbFactory = CrearInMemoryFactory();
        var passwordHasher = new PasswordHasherService();

        await using (var dbSeed = await dbFactory.CreateDbContextAsync())
        {
            var rol = new Rol { Id = 1, Nombre = "Cajero" };
            var sucursal = new Sucursal { Id = 1, Nombre = "Sucursal Central", EsCentral = true };
            dbSeed.Usuarios.Add(new Usuario
            {
                Id = 50,
                Ci = "cajero1",
                NombreCompleto = "Cajero Legacy",
                PasswordHash = "clavePlana123",
                Estado = EstadosUsuario.Activo,
                FechaInicio = DateTime.UtcNow,
                Rol = rol,
                Sucursal = sucursal
            });
            await dbSeed.SaveChangesAsync();
        }

        // Act
        await using (var dbMigracion = await dbFactory.CreateDbContextAsync())
        {
            DbSeeder.SeedInicial(dbMigracion, passwordHasher);
        }

        // Assert
        await using var dbVerificacion = await dbFactory.CreateDbContextAsync();
        var usuarioMigrado = await dbVerificacion.Usuarios.FirstAsync(u => u.Ci == "cajero1");
        Assert.True(PasswordHasher.IsHashed(usuarioMigrado.PasswordHash));
        Assert.NotEqual("clavePlana123", usuarioMigrado.PasswordHash);
        Assert.True(PasswordHasher.VerifyPassword("clavePlana123", usuarioMigrado.PasswordHash));
    }

    private static IDbContextFactory<FerreteriaDbContext> CrearInMemoryFactory()
    {
        var dbName = $"FerreteriaLoginTests_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<FerreteriaDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new InMemoryDbContextFactory(options);
    }

    private sealed class InMemoryDbContextFactory : IDbContextFactory<FerreteriaDbContext>
    {
        private readonly DbContextOptions<FerreteriaDbContext> _options;

        public InMemoryDbContextFactory(DbContextOptions<FerreteriaDbContext> options)
        {
            _options = options;
        }

        public FerreteriaDbContext CreateDbContext()
        {
            return new FerreteriaDbContext(_options);
        }

        public Task<FerreteriaDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new FerreteriaDbContext(_options));
        }
    }

    private sealed class UsuarioRepositoryEspia : IUsuarioRepository
    {
        public int LlamadasObtenerPorCi { get; private set; }

        public Task<Usuario?> ObtenerPorCiAsync(string ci, CancellationToken ct = default)
        {
            LlamadasObtenerPorCi++;
            return Task.FromResult<Usuario?>(null);
        }

        public Task<bool> ExisteCiAsync(string ci, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Usuario>> ObtenerActivosPorSucursalAsync(int idSucursal, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Usuario>> ObtenerTodosAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Usuario>> BuscarAsync(Expression<Func<Usuario, bool>> predicado, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<bool> ExisteAsync(Expression<Func<Usuario, bool>> predicado, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<int> ContarAsync(Expression<Func<Usuario, bool>>? predicado = null, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Usuario> AgregarAsync(Usuario entidad, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task ActualizarAsync(Usuario entidad, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task EliminarAsync(Usuario entidad, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}
