using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;

namespace Ali25_V10.Data.Modelos;
public class Z900_Bitacora
{
    [Key]
    [StringLength(65)]
    public string BitacoraId { get; set; } = MyFunc.MyGuid("Bit");
    public DateTime Fecha { get; set; } = DateTime.Now;
    [StringLength(65)]
    public string UserId { get; set; } = "";
    public string Desc { get; set; } = "";
    [StringLength(65)]
    public string OrgId { get; set; } = "";
    public Z900_Bitacora(string userId, string desc, string orgId)
    {
        UserId = userId;
        Desc = desc;
        OrgId = orgId;
    }
    
   

    
}