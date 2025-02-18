using System.Threading;
using System.Threading.Tasks;
using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;

namespace Ali25_V10.Data.Sistema;
public interface IRepoBitacora
{
    // Bitácora: Para acciones de usuarios (siempre tiene OrgId y UserId)
    Task<ApiRespAll<Z900_Bitacora>> GetBitacoraFiltrada(
        string orgId, 
        FiltroBitacora filtro, 
        bool byPassCache = false, 
        CancellationToken cancellationToken = default, 
        ApplicationUser elUser = null);
        
    Task AddBitacora(
        string userId, 
        string desc, 
        string orgId, 
        CancellationToken cancellationToken = default);

    // Logs: Para acciones del sistema (puede no tener OrgId o UserId)
    Task AddLog(
        string userId,      // Usuario que genera el log (opcional)
        string orgId ,       // Organización relacionada (opcional)
        string desc,                    // Descripción del log
        string tipoLog = "Info",        // Info, Warning, Error, Debug, Security, Audit, Performance
        string origen = "Sistema",      // Módulo o proceso que genera el log
        CancellationToken cancellationToken = default);
} 