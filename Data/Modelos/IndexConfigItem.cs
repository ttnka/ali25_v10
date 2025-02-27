using System.ComponentModel.DataAnnotations;
using Ali25_V10.Data.Sistema;

namespace Ali25_V10.Data.Modelos
{
    public class IndexConfigItem
    {
        [Key]
        [StringLength(65)]
        public string ConfigId { get; set; } = MyFunc.MyGuid("Cfg");
        
        [Required]
        [StringLength(50)]
        public string Grupo { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Tipo { get; set; } = string.Empty;
        
        [Required]
        [StringLength(250)]
        public string Valor { get; set; } = string.Empty;
        
        public int Estado { get; set; } = 5;
        public bool Status { get; set; } = true;
    }
} 