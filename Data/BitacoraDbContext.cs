using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;

namespace Ali25_V10.Data;

public class BitacoraDbContext : DbContext
{
    public BitacoraDbContext(DbContextOptions<BitacoraDbContext> options) 
        : base(options)
    {
    }

    public DbSet<ZConfig> Configuraciones { get; set; }
    public DbSet<Z900_Bitacora> Bitacoras { get; set; }
    public DbSet<Z910_Log> Log { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }
}
