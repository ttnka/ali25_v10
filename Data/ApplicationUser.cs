using Microsoft.AspNetCore.Identity;
using Ali25_V10.Data.Modelos;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Ali25_V10.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        // Propiedades adicionales personalizadas
        [StringLength(65)]
        public string Nombre { get; set; } = string.Empty;
        [StringLength(65)]
        public string Paterno { get; set; } = string.Empty;
        [StringLength(65)]
        public string Materno { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public int Nivel { get; set; } = 1;
        public bool EsActivo { get; set; }
        public int Estado { get; set; } = 5;
        public bool Status { get; set; } = true;
    
        // Relación con Org (misma base de datos)
        [StringLength(65)]
        public string OrgId { get; set; } = string.Empty;
        public virtual W100_Org? Org { get; set; }
        public string UserId => Id;
    
        // Constructor
        public ApplicationUser()
        {
            FechaRegistro = DateTime.Now;
            EsActivo = true;
        }
    }
}
