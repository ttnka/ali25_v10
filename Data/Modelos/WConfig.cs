using Ali25_V10.Data.Sistema;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ali25_V10.Data.Modelos;

public class WConfig
{
    [Key]
    [StringLength(65, ErrorMessage = "El ID no puede exceder los 65 caracteres")]
    public string ConfigId { get; set; } = MyFunc.MyGuid("WConfig");

    [Required(ErrorMessage = "El grupo es obligatorio")]
    [StringLength(25, MinimumLength = 3, 
        ErrorMessage = "El grupo debe tener entre 3 y 25 caracteres")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", 
        ErrorMessage = "El grupo solo puede contener letras, números, guiones y guiones bajos")]
    public string Grupo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La clave es obligatoria")]
    [StringLength(50, MinimumLength = 3, 
        ErrorMessage = "La clave debe tener entre 3 y 50 caracteres")]
    [RegularExpression(@"^[a-zA-Z0-9_.-]+$", 
        ErrorMessage = "La clave solo puede contener letras, números, puntos, guiones y guiones bajos")]
    public string Clave { get; set; } = string.Empty;

    [Required(ErrorMessage = "El título es obligatorio")]
    [StringLength(25, MinimumLength = 3, 
        ErrorMessage = "El título debe tener entre 3 y 25 caracteres")]
    public string Titulo { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "La descripción no puede exceder los 1000 caracteres")]
    public string Descripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo es obligatorio")]
    [StringLength(2, MinimumLength = 2, 
        ErrorMessage = "El tipo debe tener exactamente 2 caracteres")]
    [RegularExpression(@"^[A-Z]{2}$", 
        ErrorMessage = "El tipo debe ser dos letras mayúsculas")]
    public string Tipo { get; set; } = string.Empty;
    public int Numero { get; set; } = 0;
    public string Texto { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.Now;

    public bool EsGrupo { get; set; } = false;
    
    public bool Global { get; set; } = false;

    [Required(ErrorMessage = "La organización es obligatoria")]
    [StringLength(65, ErrorMessage = "El ID de organización no puede exceder los 65 caracteres")]
    public string OrgId { get; set; } = string.Empty;

    [Range(0, 9, ErrorMessage = "El estado debe estar entre 0 y 9")]
    public int Estado { get; set; } = 5;
    
    public bool Status { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    // Constructor sin parámetros requerido por EF Core
    public WConfig() { }

    // Constructor principal
    public WConfig(
        string grupo,
        string clave,
        string titulo,
        string descripcion,
        string tipo,
        bool esGrupo,
        int numero,
        string texto,
        DateTime fecha,
        bool global,
        string orgId,
        int estado = 5)
    {
        if (string.IsNullOrWhiteSpace(grupo))
            throw new ArgumentException("El grupo es obligatorio", nameof(grupo));
        if (string.IsNullOrWhiteSpace(clave))
            throw new ArgumentException("La clave es obligatoria", nameof(clave));
        if (string.IsNullOrWhiteSpace(titulo))
            throw new ArgumentException("El título es obligatorio", nameof(titulo));
        if (string.IsNullOrWhiteSpace(tipo))
            throw new ArgumentException("El tipo es obligatorio", nameof(tipo));
        if (string.IsNullOrWhiteSpace(orgId))
            throw new ArgumentException("La organización es obligatoria", nameof(orgId));
        if (estado < 0 || estado > 9)
            throw new ArgumentException("El estado debe estar entre 0 y 9", nameof(estado));

        Grupo = grupo.Length > 25 ? grupo[..25] : grupo;
        Clave = clave.Length > 50 ? clave[..50] : clave;
        Titulo = titulo.Length > 25 ? titulo[..25] : titulo;
        Descripcion = descripcion.Length > 1000 ? descripcion[..1000] : descripcion;
        Tipo = tipo.Length > 2 ? tipo[..2] : tipo;
        Numero = numero;
        Texto = texto;
        Fecha = fecha;
        EsGrupo = esGrupo;
        Global = global;
        OrgId = orgId;
        Estado = estado;
    }
}
