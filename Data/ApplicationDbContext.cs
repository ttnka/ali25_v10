using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;

namespace Ali25_V10.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : 
        IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<W100_Org> Organizaciones => Set<W100_Org>();
        public DbSet<W180_Files> Archivos => Set<W180_Files>();
        public DbSet<W210_Clientes> Clientes => Set<W210_Clientes>();
        public DbSet<W290_Formatos> Formatos => Set<W290_Formatos>();
        public DbSet<W291_FormatoGpo> FormatosGrupos => Set<W291_FormatoGpo>();
        public DbSet<W220_Folios> Folios => Set<W220_Folios>();
        public DbSet<W222_FolioDet> FoliosDet => Set<W222_FolioDet>();
        public DbSet<WConfig> Configuraciones => Set<WConfig>();
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Org)
                .WithMany()
                .HasForeignKey(u => u.OrgId);
        }
    }

    
}
