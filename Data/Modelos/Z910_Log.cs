using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ali25_V10.Data.Sistema;

namespace Ali25_V10.Data.Modelos;
public class Z910_Log
{
    [Key]
    [StringLength(65)]
    public string LogId { get; set; } = MyFunc.MyGuid("Logs");
    public DateTime Fecha { get; set; } = DateTime.Now;
    [StringLength(65)]
    public string UserId { get; set; } = "Sistema";
    [StringLength(65)]
    public string OrgId { get; set; } = "Sistema";
    public string Desc { get; set; } = "";
    /// <summary>
    /// Tipos de log posibles:
    /// - Info: Información general del sistema
    /// - Warning: Advertencias que no detienen la operación
    /// - Error: Errores que impiden completar una operación
    /// - Debug: Información detallada para desarrollo
    /// - Security: Eventos relacionados con seguridad
    /// - Audit: Cambios importantes en datos
    /// - Performance: Eventos de rendimiento
    /// </summary>
    [StringLength(20)]
    public string TipoLog { get; set; } = "Info";
    /// <summary>
    /// Campo Origen indica la fuente del log:
    /// - Nombre del módulo (ej: "Folios", "Usuarios", "Reportes")
    /// - Nombre del proceso (ej: "ImportaciónDatos", "GeneraciónPDF")
    /// - Nombre del método (ej: "CrearFolio", "ActualizarUsuario")
    /// - Nombre del servicio (ej: "AuthService", "EmailService")
    /// - Ubicación en código (ej: "Controllers/FolioController")
    /// </summary>
    [StringLength(100)]
    public string Origen { get; set; } = "";

    public Z910_Log(string userId, string orgId, string desc, string tipoLog, string origen)
    {
        UserId = userId;
        OrgId = orgId;
        Desc = desc;
        TipoLog = tipoLog;
        Origen = origen;
    }
}