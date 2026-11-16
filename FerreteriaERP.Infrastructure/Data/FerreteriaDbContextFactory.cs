using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FerreteriaERP.Infrastructure.Data
{
    public class FerreteriaDbContextFactory : IDesignTimeDbContextFactory<FerreteriaDbContext>
    {
        public FerreteriaDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<FerreteriaDbContext>();
            
            // Cadena de conexión a tu base de datos local
            var connectionString = "Host=localhost;Port=5432;Database=FerreteriaDSS;Username=postgres;Password=kali";
            
            optionsBuilder.UseNpgsql(connectionString);

            return new FerreteriaDbContext(optionsBuilder.Options);
        }
    }
}