using System.ComponentModel.DataAnnotations;
using Ali25_V10.Data.Sistema;
namespace Ali25_V10.Data.Modelos;

public class W210_Clientes
{
    [Key]
    [StringLength(65)]
    public string ClienteId { get; set; } = MyFunc.MyGuid("Org");
    [StringLength(65)]
    public string OrgId { get; set; } = string.Empty;
    [StringLength(65)]
    public string ClienteOrgId { get; set; } = string.Empty;
    
    public int Estado { get; set; } = 5;
    public bool Status { get; set; } = true;
    
    public W210_Clientes(string orgId, string clienteOrgId)
        {   
            OrgId = orgId;
            ClienteOrgId = clienteOrgId;
            
        }
        
}

