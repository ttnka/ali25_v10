using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ali25_V10.Data.Sistema;
namespace Ali25_V10.Data.Modelos;

public class W292_FormatoDet
{
    [Key]
    [StringLength(65)]
    public string FormatoDetId { get; set; } = MyFunc.MyGuid("FormatoDet");
    [Required(ErrorMessage = "Formato es obligatorio")]
    [StringLength(65)]
    public string FormatoId { get; set; } = string.Empty;
    public int Orden { get; set; } = 0;
    
    public string Tipo { get; set; } = string.Empty;
    public string Campo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int Estado { get; set; } = 5;
    public bool Status { get; set; } = true;
    
    public W292_FormatoDet(string formatoId, int orden, string tipo, string campo, string descripcion, int estado, bool status, DateTime fechaCaptura)
    {
        FormatoId = formatoId;  
        Orden = orden;
        Tipo = tipo;
        Campo = campo;
        Descripcion = descripcion;
        Estado = estado;
        Status = status;
    }
}

