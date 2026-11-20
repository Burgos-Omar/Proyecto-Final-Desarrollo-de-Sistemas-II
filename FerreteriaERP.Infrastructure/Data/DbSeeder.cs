using System;
using System.Linq;
using FerreteriaERP.Core.Constants;
using FerreteriaERP.Core.Entities;
using FerreteriaERP.Core.Interfaces;

namespace FerreteriaERP.Infrastructure.Data
{
    /// <summary>
    /// Inicializador de datos base (semilla) para garantizar el acceso inicial y datos de prueba del POS.
    /// </summary>
    public static class DbSeeder
    {
        public static void SeedInicial(FerreteriaDbContext context, IPasswordHasherService passwordHasher)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(passwordHasher);

            var huboCambios = false;

            // Garantizar que exista la Sucursal Central (requerida tanto por el Admin como por el Stock inicial)
            var sucursalCentral = context.Sucursales.FirstOrDefault(s => s.EsCentral);
            if (sucursalCentral is null)
            {
                sucursalCentral = new Sucursal
                {
                    Nombre = "Sucursal Central",
                    EsCentral = true
                };
                context.Sucursales.Add(sucursalCentral);
                huboCambios = true;
            }

            // 1. Semilla de Usuario Administrador (solo si la tabla Usuarios está vacía)
            if (!context.Usuarios.Any())
            {
                var rolAdmin = context.Roles.FirstOrDefault(r => r.Nombre == "Admin")
                               ?? new Rol
                               {
                                   Nombre = "Admin"
                               };

                var usuarioAdmin = new Usuario
                {
                    Ci = "admin",
                    NombreCompleto = "Super Administrador",
                    PasswordHash = passwordHasher.HashPassword("admin123"),
                    Estado = EstadosUsuario.Activo,
                    Salario = 0m,
                    FechaInicio = DateTime.UtcNow, // Npgsql exige UTC para timestamp with time zone
                    Rol = rolAdmin,
                    Sucursal = sucursalCentral
                };

                context.Usuarios.Add(usuarioAdmin);
                huboCambios = true;
            }

            // 2. Semilla de Catálogo e Inventario de Prueba (solo si la tabla Productos está vacía)
            if (!context.Productos.Any())
            {
                var categoriaHerramientas = context.Categorias.FirstOrDefault(c => c.Nombre == "Herramientas")
                                            ?? new Categoria
                                            {
                                                Nombre = "Herramientas"
                                            };

                var proveedorSur = context.Proveedores.FirstOrDefault(p => p.NombreEmpresa == "Importadora Sur")
                                   ?? new Proveedor
                                   {
                                       NombreEmpresa = "Importadora Sur"
                                   };

                var martillo = new Producto
                {
                    Nombre = "Martillo de Acero",
                    CodigoBarras = "1111",
                    PrecioCompra = 30.00m,
                    PrecioVenta = 45.00m,
                    StockMinimo = 5,
                    Categoria = categoriaHerramientas,
                    Proveedor = proveedorSur
                };

                var taladro = new Producto
                {
                    Nombre = "Taladro Percutor 700W",
                    CodigoBarras = "2222",
                    PrecioCompra = 250.00m,
                    PrecioVenta = 320.00m,
                    StockMinimo = 2,
                    Categoria = categoriaHerramientas,
                    Proveedor = proveedorSur
                };

                context.StockSucursales.AddRange(
                    new StockSucursal
                    {
                        Producto = martillo,
                        Sucursal = sucursalCentral,
                        CantidadActual = 50
                    },
                    new StockSucursal
                    {
                        Producto = taladro,
                        Sucursal = sucursalCentral,
                        CantidadActual = 50
                    });

                huboCambios = true;
            }

            // 3. Semilla de Vendedores Iniciales para el POS y Ranking (PASO 14)
            if (!context.Empleados.Any())
            {
                context.Empleados.AddRange(
                    new Empleado
                    {
                        Nombre = "Carlos",
                        Apellidos = "Mendoza Ríos",
                        Ci = "7120458",
                        Telefono = "59171234567",
                        Direccion = "Av. Las Américas #450",
                        Salario = 3200.00m,
                        Puesto = "Vendedor",
                        EstaActivo = true,
                        Sucursal = sucursalCentral
                    },
                    new Empleado
                    {
                        Nombre = "Lucía",
                        Apellidos = "Vargas Quiroga",
                        Ci = "8341920",
                        Telefono = "59172987654",
                        Direccion = "B. Senac C. 4 #120",
                        Salario = 3200.00m,
                        Puesto = "Vendedor",
                        EstaActivo = true,
                        Sucursal = sucursalCentral
                    });

                huboCambios = true;
            }

            // 4. Semilla de Promoción Vigente para la pantalla de Inicio (PASO 15)
            if (!context.Promociones.Any())
            {
                var ahoraUtc = DateTime.UtcNow;
                context.Promociones.Add(new Promocion
                {
                    Titulo = "Mes del Constructor: 15% OFF en Herramientas Eléctricas",
                    Descripcion = "Descuento especial en taladros percutores, amoladoras y kits profesionales por compras al contado o QR.",
                    FechaInicio = ahoraUtc.AddDays(-5),
                    FechaFin = ahoraUtc.AddDays(25),
                    ImagenFondoUrl = null,
                    EstaActiva = true
                });

                huboCambios = true;
            }

            if (huboCambios)
            {
                context.SaveChanges();
            }
        }
    }
}
