namespace Ali25_V10.Data.Sistema;
public class FiltroBitacora 
        {
            public bool Rango { get; set; } = true;
            public bool Ascen { get; set; } = true;
            public DateTime? FechaInicio { get; set; }
            public DateTime? FechaFin { get; set; }
            public string? OrgId { get; set; }
            public string? UsuarioId { get; set; }
            public string? Desc { get; set; }
            public bool Activo { get; set; } = false;
            
        }