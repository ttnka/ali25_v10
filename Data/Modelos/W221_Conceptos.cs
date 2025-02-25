using System.ComponentModel.DataAnnotations;
using Ali25_V10.Data.Sistema;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Ali25_V10.Data.Modelos;

public class W221_Conceptos
{
    [Key]
    [StringLength(65)]
    public string ConceptoId { get; set; } = MyFunc.MyGuid("Concepto");

    [StringLength(65)]
    public string FolioId { get; set; } = string.Empty;
    [StringLength(65)]
    public string ProductoId { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Cantidad { get; set; } = 0M;
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Precio { get; set; } = 0M;
    public int Estado { get; set; } = 5;
    public bool Status { get; set; } = true;
    public W221_Conceptos(string folioId, string productoId, 
            decimal cantidad, decimal precio, int estado, bool status)
    {
        FolioId = folioId;
        ProductoId = productoId;
        Cantidad = cantidad;
        Precio = precio;
        Estado = estado;
        Status = status;
    }
    [NotMapped]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Importe => Cantidad * Precio;
}       

