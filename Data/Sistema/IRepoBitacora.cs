using System.Threading;
using System.Threading.Tasks;
using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;

namespace Ali25_V10.Data.Sistema;

public interface IRepoBitacora
{
    
    Task<ApiRespAll<Z900_Bitacora>> GetBitacoraFiltrada(
        string orgId,
        ApplicationUser elUser,
        FiltroBitacora filtro,
        bool byPassCache = false,
        CancellationToken cancellationToken = default);

    Task AddBitacora(
        string userId,
        string desc,
        string orgId,
        CancellationToken cancellationToken = default);

    Task AddLog(
        string desc,
        string tipoLog = "Info",
        string origen = "Sistema",
        string userId = "Sistema",
        string orgId = "Sistema",
        CancellationToken cancellationToken = default);
} 