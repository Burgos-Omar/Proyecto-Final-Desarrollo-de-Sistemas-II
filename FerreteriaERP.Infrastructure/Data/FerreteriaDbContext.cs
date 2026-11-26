using FerreteriaERP.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FerreteriaERP.Infrastructure.Data
{
    public class FerreteriaDbContext : DbContext
    {
        // El constructor recibe las opciones de conexión
        public FerreteriaDbContext(DbContextOptions<FerreteriaDbContext> options) : base(options)
        {
        }

        // DbSets: Representan las tablas en la base de datos
        public DbSet<Rol> Roles { get; set; } = null!;
        public DbSet<Sucursal> Sucursales { get; set; } = null!;
        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Categoria> Categorias { get; set; } = null!;
        public DbSet<Proveedor> Proveedores { get; set; } = null!;
        public DbSet<Producto> Productos { get; set; } = null!;
        public DbSet<StockSucursal> StockSucursales { get; set; } = null!;
        public DbSet<Cliente> Clientes { get; set; } = null!;
        public DbSet<Venta> Ventas { get; set; } = null!;
        public DbSet<DetalleVenta> DetalleVentas { get; set; } = null!;
        public DbSet<Credito> Creditos { get; set; } = null!;
        public DbSet<AbonoCredito> AbonosCredito { get; set; } = null!;
        public DbSet<Entrega> Entregas { get; set; } = null!;
        public DbSet<CargaEntrante> CargasEntrantes { get; set; } = null!;
        public DbSet<OrdenDespacho> OrdenesDespacho { get; set; } = null!;
        public DbSet<Empleado> Empleados { get; set; } = null!;
        public DbSet<Promocion> Promociones { get; set; } = null!;
        public DbSet<Cotizacion> Cotizaciones { get; set; } = null!;
        public DbSet<DetalleCotizacion> DetalleCotizaciones { get; set; } = null!;
        public DbSet<GastoOperativo> GastosOperativos { get; set; } = null!;
        public DbSet<OrdenCompra> OrdenesCompra { get; set; } = null!;
        public DbSet<DetalleOrdenCompra> DetallesOrdenCompra { get; set; } = null!;
        public DbSet<ConfiguracionNegocio> ConfiguracionesNegocio { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mapeo exacto de los nombres de tablas en PostgreSQL (snake_case)
            modelBuilder.Entity<StockSucursal>().ToTable("stock_sucursales");
            modelBuilder.Entity<DetalleVenta>().ToTable("detalle_ventas");
            modelBuilder.Entity<AbonoCredito>().ToTable("abonos_credito");
            modelBuilder.Entity<CargaEntrante>().ToTable("cargas_entrantes");
            modelBuilder.Entity<OrdenDespacho>().ToTable("ordenes_despacho");
            modelBuilder.Entity<DetalleCotizacion>().ToTable("detalle_cotizaciones");
            modelBuilder.Entity<GastoOperativo>().ToTable("gastos_operativos");
            modelBuilder.Entity<OrdenCompra>().ToTable("ordenes_compra");
            modelBuilder.Entity<DetalleOrdenCompra>().ToTable("detalles_orden_compra");
            modelBuilder.Entity<ConfiguracionNegocio>().ToTable("configuraciones_negocio");

            // Configurar campos únicos para evitar duplicados a nivel de base de datos
            modelBuilder.Entity<Usuario>().HasIndex(u => u.Ci).IsUnique();
            modelBuilder.Entity<Producto>().HasIndex(p => p.CodigoBarras).IsUnique();
            modelBuilder.Entity<StockSucursal>()
                .HasIndex(s => new { s.IdProducto, s.IdSucursal })
                .IsUnique();

            // Mapeo explícito de claves foráneas (prefijo Id*) para evitar columnas sombra duplicadas en EF Core
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasOne(u => u.Rol)
                    .WithMany(r => r.Usuarios)
                    .HasForeignKey(u => u.IdRol)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(u => u.Sucursal)
                    .WithMany(s => s.Usuarios)
                    .HasForeignKey(u => u.IdSucursal)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Producto>(entity =>
            {
                entity.HasOne(p => p.Categoria)
                    .WithMany(c => c.Productos)
                    .HasForeignKey(p => p.IdCategoria)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Proveedor)
                    .WithMany(pr => pr.Productos)
                    .HasForeignKey(p => p.IdProveedor)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StockSucursal>(entity =>
            {
                entity.HasOne(s => s.Producto)
                    .WithMany(p => p.Stocks)
                    .HasForeignKey(s => s.IdProducto)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.Sucursal)
                    .WithMany(su => su.Stocks)
                    .HasForeignKey(s => s.IdSucursal)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Venta>(entity =>
            {
                entity.HasOne(v => v.Usuario)
                    .WithMany()
                    .HasForeignKey(v => v.IdUsuario)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(v => v.Sucursal)
                    .WithMany(s => s.Ventas)
                    .HasForeignKey(v => v.IdSucursal)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(v => v.Cliente)
                    .WithMany(c => c.Ventas)
                    .HasForeignKey(v => v.IdCliente)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(v => v.Empleado)
                    .WithMany(e => e.Ventas)
                    .HasForeignKey(v => v.IdEmpleado)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Empleado>(entity =>
            {
                entity.HasIndex(e => e.Ci).IsUnique();

                entity.HasOne(e => e.Sucursal)
                    .WithMany(s => s.Empleados)
                    .HasForeignKey(e => e.IdSucursal)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración de borrado en cascada para DetalleVentas
            modelBuilder.Entity<DetalleVenta>(entity =>
            {
                entity.HasOne(d => d.Venta)
                    .WithMany(v => v.Detalles)
                    .HasForeignKey(d => d.IdVenta)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Producto)
                    .WithMany()
                    .HasForeignKey(d => d.IdProducto)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Promocion)
                    .WithMany()
                    .HasForeignKey(d => d.IdPromocion)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Credito>(entity =>
            {
                entity.HasOne(c => c.Venta)
                    .WithMany()
                    .HasForeignKey(c => c.IdVenta)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.Cliente)
                    .WithMany(cl => cl.Creditos)
                    .HasForeignKey(c => c.IdCliente)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AbonoCredito>(entity =>
            {
                entity.HasOne(a => a.Credito)
                    .WithMany(c => c.Abonos)
                    .HasForeignKey(a => a.IdCredito)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Entrega>(entity =>
            {
                entity.HasOne(e => e.Venta)
                    .WithMany()
                    .HasForeignKey(e => e.IdVenta)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Chofer)
                    .WithMany()
                    .HasForeignKey(e => e.IdChofer)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CargaEntrante>(entity =>
            {
                entity.HasOne(c => c.Proveedor)
                    .WithMany()
                    .HasForeignKey(c => c.IdProveedor)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.Producto)
                    .WithMany()
                    .HasForeignKey(c => c.IdProducto)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(c => c.Sucursal)
                    .WithMany()
                    .HasForeignKey(c => c.IdSucursal)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<OrdenDespacho>(entity =>
            {
                entity.HasOne(o => o.Venta)
                    .WithMany()
                    .HasForeignKey(o => o.IdVenta)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DetalleCotizacion>(entity =>
            {
                entity.HasOne(d => d.Cotizacion)
                    .WithMany(c => c.Detalles)
                    .HasForeignKey(d => d.IdCotizacion)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Producto)
                    .WithMany()
                    .HasForeignKey(d => d.IdProducto)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Promocion)
                    .WithMany()
                    .HasForeignKey(d => d.IdPromocion)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<GastoOperativo>(entity =>
            {
                entity.HasOne(g => g.Sucursal)
                    .WithMany()
                    .HasForeignKey(g => g.IdSucursal)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Promocion>(entity =>
            {
                entity.HasOne(p => p.Producto)
                    .WithMany()
                    .HasForeignKey(p => p.IdProducto)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(p => p.Categoria)
                    .WithMany()
                    .HasForeignKey(p => p.IdCategoria)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(p => p.Sucursal)
                    .WithMany()
                    .HasForeignKey(p => p.IdSucursal)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<OrdenCompra>(entity =>
            {
                entity.HasIndex(o => o.NumeroOrden).IsUnique();

                entity.HasOne(o => o.Proveedor)
                    .WithMany()
                    .HasForeignKey(o => o.IdProveedor)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.Sucursal)
                    .WithMany()
                    .HasForeignKey(o => o.IdSucursal)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.Usuario)
                    .WithMany()
                    .HasForeignKey(o => o.IdUsuario)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<DetalleOrdenCompra>(entity =>
            {
                entity.HasOne(d => d.OrdenCompra)
                    .WithMany(o => o.Detalles)
                    .HasForeignKey(d => d.IdOrdenCompra)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Producto)
                    .WithMany()
                    .HasForeignKey(d => d.IdProducto)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
