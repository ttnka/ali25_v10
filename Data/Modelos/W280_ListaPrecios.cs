using Ali25_V10.Data.Sistema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ali25_V10.Data.Modelos;

public class W280_ListaPrecios
{
    [Key]
    [StringLength(65)]
    public string ListaPrecioId { get; set; } = MyFunc.MyGuid("LPrecio");
    public bool EsGlobal { get; set; } = false;
    [StringLength(25)]
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;

    [StringLength(65)]
    public string OrgId { get; set; } = string.Empty;

    public int Estado { get; set; } = 5;
    public bool Status { get; set; } = true;

    public W280_ListaPrecios(bool esGlobal, string titulo, 
            string descripcion, string orgId, int estado, bool status = true)
    {
        ListaPrecioId = MyFunc.MyGuid("LPrecio");
        EsGlobal = esGlobal;
        Titulo = titulo;
        Descripcion = descripcion;
        OrgId = orgId;
        Estado = estado;
        Status = status;
    }

}

