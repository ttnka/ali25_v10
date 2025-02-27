using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Ali25_V10.Data.Modelos;

public class W222_FolioDet
{
    [Key]
    [StringLength(65)]
    public string FolioId { get; set; } = string.Empty;
    public string Campo { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public int Estado { get; set; } = 5;
    public bool Status { get; set; } = true;
    

    public W222_FolioDet(string folioId, string campo, string valor, 
            int estado, bool status)
    {
        FolioId = folioId;
        Campo = campo;
        Valor = valor;
        Estado = estado;
        Status = status;
        
    }
}
