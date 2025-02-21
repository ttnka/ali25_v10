using System.ComponentModel.DataAnnotations;
using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;
namespace Ali25_V10.Data.Modelos;

public class W220_Folios
{
    [Key]
    [StringLength(65)]
    public string FolioId { get; set; } = MyFunc.MyGuid("Folio");
    [Required(ErrorMessage = "Folio es obligatorio")]
    [StringLength(10, MinimumLength = 5, ErrorMessage = "Folio debe tener 5 digitos")]
    public string Folio { get; set; } = "";
    [Required(ErrorMessage = "Fecha Folio es obligatorio")]
    public DateTime FechaFolio { get; set; } = DateTime.Now;
    public DateTime FechaCaptura { get; set; } = DateTime.Now;
    [Required(ErrorMessage = "Cliente es obligatorio")]
    [StringLength(65)]
    public string ClienteId { get; set; } = string.Empty;
    [StringLength(65)]
    public string FormatoId { get; set; } = string.Empty;
    [StringLength(65)]
    public string OrgId { get; set; } = string.Empty;
    public int Estado { get; set; } = 5;
    public bool Status { get; set; } = true;
        
    public W220_Folios(DateTime fechaFolio, DateTime fechaCaptura, 
                        string clienteId, string formatoId, string orgId, int estado, bool status)
        {   
            FechaFolio = fechaFolio; 
            FechaCaptura = fechaCaptura; 
            ClienteId = clienteId; 
            FormatoId = formatoId;
            OrgId = orgId;
            Estado = estado; 
            Status = status;
        }
        
}

