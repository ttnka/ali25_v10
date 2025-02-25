using Ali25_V10.Data.Sistema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ali25_V10.Data.Modelos;

public class W282_Precios
{
    [Key]
    [StringLength(65)]
    public string PrecioId { get; set; } = MyFunc.MyGuid("Precio");
    [StringLength(65)]
    public string ListaPrecioId { get; set; } = string.Empty;
    [StringLength(65)]
    public string ProductoId { get; set; } = string.Empty;
    [Column(TypeName = "decimal(10,2)")]
    public decimal Precio { get; set; } = 0M;      
    public int Estado { get; set; } = 5;
    public bool Status { get; set; } = true;
    public W282_Precios(string listaPrecioId, string productoId,
                 decimal precio, int estado, bool status = true)
    {
        ListaPrecioId = listaPrecioId;
        ProductoId = productoId;
        Precio = precio;
        Estado = estado;
        Status = status;
    }
   
}

