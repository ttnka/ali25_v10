using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ali25_V10.Data.Sistema;

namespace Ali25_V10.Data.Modelos;

public class W180_Files
{
        [Key]
        [StringLength(65)]
        public string FileId { get; set; } = MyFunc.MyGuid("Files");
        public DateTime Fecha { get; set; } = DateTime.Now;
        [StringLength(50)]
        public string Fuente { get; set; } = string.Empty;
        [StringLength(65)]
        public string FuenteId { get; set; } = string.Empty;
        [StringLength(50)]
        public string Tipo { get; set; } = string.Empty;
        [StringLength(255)]
        public string Archivo { get; set; } = string.Empty;
        [StringLength(255)]
        public string Ruta { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Gpo { get; set; }
        [StringLength(65)]
        public string OrgId { get; set; } = string.Empty;
        public int Estado { get; set; } = 5;
        public bool Status { get; set; } = true;
    
}