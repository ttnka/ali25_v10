using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ali25_V10.Data.Sistema;
namespace Ali25_V10.Data.Modelos;

public class W290_Formatos
{
    [Key]
    [StringLength(65)]
    public string FormatoId { get; set; } = MyFunc.MyGuid("Formato");
    [Required(ErrorMessage = "Nombre de Formato es obligatorio")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Nombre de Formato debe tener 3 caracteres")]
    public string FormatoNombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    [StringLength(65)]
    public string OrgId { get; set; } = string.Empty;
    public bool Global { get; set; } = false;
    public int Estado { get; set; } = 5;
    public bool Status { get; set; } = true;
    public W290_Formatos(string formatoNombre, string descripcion, string orgId, bool global, int estado, bool status)
    {
        FormatoNombre = formatoNombre;
        Descripcion = descripcion;
        OrgId = orgId;
        Global = global;
        Estado = estado;
        Status = status;
    }
}
