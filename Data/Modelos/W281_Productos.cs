using Ali25_V10.Data.Sistema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ali25_V10.Data.Modelos;

public class W281_Productos
{
    [Key]
    [StringLength(65)]
    public string ProductoId { get; set; } = MyFunc.MyGuid("Prod");
    public bool Grupo { get; set; } = false;
    
    [StringLength(15)]
    public string Clave { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string Titulo { get; set; } = string.Empty;
    
    public string Descripcion { get; set; } = string.Empty;

    [StringLength(25)]
    public string UnidadMedida { get; set; } = string.Empty;

    public int Estado { get; set; } = 5;
    public bool Status { get; set; } = true;
    public W281_Productos(bool grupo, string clave, string titulo, string descripcion, 
                            string unidadMedida, int estado, bool status = true)
    {
        ProductoId = MyFunc.MyGuid("Prod");
        Grupo = grupo;
        Clave = clave;
        Titulo = titulo;
        Descripcion = descripcion;
        UnidadMedida = unidadMedida;
        Estado = estado;
        Status = status;
    }

}

