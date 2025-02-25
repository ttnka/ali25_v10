using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ali25_V10.Data.Sistema;
namespace Ali25_V10.Data.Modelos;

public class W291_FormatoGpo
{
    [Key]
    [StringLength(65)]
    public string FormatoGpoId { get; set; } = MyFunc.MyGuid("FormatoGpo");
    [Required(ErrorMessage = "Formato es obligatorio")]
    [StringLength(65)]
    public string FormatoId { get; set; } = string.Empty;
    [StringLength(65)]
    public string OrgId { get; set; } = string.Empty;
    public int Estado { get; set; } = 5;
    public bool Status { get; set; } = true;

    public W291_FormatoGpo(string formatoId, string orgId, int estado, bool status)
    {
        FormatoId = formatoId;
        OrgId = orgId;
        Estado = estado;
        Status = status;
    }
}
