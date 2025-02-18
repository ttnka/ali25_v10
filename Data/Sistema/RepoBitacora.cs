using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Ali25_V10.Data.Sistema
{
    public class RepoBitacora : IRepoBitacora
    {
        private readonly BitacoraDbContext _context;
        private readonly ILogger<RepoBitacora> _logger;
        private readonly IMemoryCache _globalCache;
        private readonly SemaphoreSlim semaphore = new(1, 1);
        private const int Min_actualizar = 5;

        public RepoBitacora(
            BitacoraDbContext context,
            ILogger<RepoBitacora> logger,
            IMemoryCache globalCache)
        {
            _context = context;
            _logger = logger;
            _globalCache = globalCache;
        }

        public async Task<ApiRespAll<Z900_Bitacora>> GetBitacoraFiltrada(
            string orgId, 
            FiltroBitacora filtro, 
            bool byPassCache = false, 
            CancellationToken cancellationToken = default, 
            ApplicationUser elUser = null)
        {
            ApiRespAll<Z900_Bitacora> respuesta = new() { Exito = false, Varios = true };
            var cacheKey = $"Bitacora_{orgId}_{filtro.FechaInicio}_{filtro.FechaFin}_{filtro.UsuarioId}_{filtro.OrgId}_{filtro.Desc}";

            if (!byPassCache && _globalCache.TryGetValue(cacheKey, out var cachedResultado))
            {
                var (cachedRespuesta, cacheTime) = ((ApiRespAll<Z900_Bitacora>, DateTime))cachedResultado;
                if ((DateTime.UtcNow - cacheTime).TotalMinutes < Min_actualizar)
                {
                    return cachedRespuesta;
                }
            }

            await semaphore.WaitAsync(cancellationToken);
            try
            {
                IQueryable<Z900_Bitacora> query = _context.Bitacoras.AsNoTracking();
                
                
                if (filtro.Rango)
                {
                    if (filtro.FechaInicio.HasValue)
                        query = query.Where(b => b.Fecha >= filtro.FechaInicio);

                    if (filtro.FechaFin.HasValue)
                        query = query.Where(b => b.Fecha <= filtro.FechaFin);
                }
                else if (filtro.FechaInicio.HasValue)
                {
                    DateTime inicioDia = filtro.FechaInicio.Value.Date;
                    DateTime finDia = inicioDia.AddDays(1).AddTicks(-1);
                    query = query.Where(b => b.Fecha >= inicioDia && b.Fecha <= finDia);
                }

                if (!string.IsNullOrEmpty(filtro.UsuarioId))
                    query = query.Where(b => b.UserId.Contains(filtro.UsuarioId));

                if (!string.IsNullOrEmpty(filtro.OrgId))
                    query = query.Where(b => b.OrgId.Contains(filtro.OrgId));

                if (!string.IsNullOrEmpty(filtro.Desc))
                    query = query.Where(b => b.Desc.Contains(filtro.Desc));

                respuesta.DataVarios = await query.ToListAsync(cancellationToken);
                respuesta.Exito = true;

                _globalCache.Set(cacheKey, (respuesta, DateTime.UtcNow), TimeSpan.FromMinutes(Min_actualizar));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al filtrar la bitácora. Usuario: {UserId}", elUser?.Id ?? "No identificado");
                respuesta.MsnError.Add($"Error al filtrar la bitácora: {ex.Message}");
                await AddLog(
                    desc: $"Error al filtrar bitácora: {ex.Message}",
                    tipoLog: "Error",
                    origen: "RepoBitacora.GetBitacoraFiltrada",
                    userId: elUser?.Id ?? "Sistema",
                    orgId: orgId,
                    cancellationToken: cancellationToken
                );
            }
            finally
            {
                semaphore.Release();
            }
            return respuesta;
        }

        public async Task AddBitacora(string userId, string desc, string orgId, CancellationToken cancellationToken = default)
        {
            try
            {
                var bit = new Z900_Bitacora(userId, desc, orgId);

                await _context.Bitacoras.AddAsync(bit, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("Log agregado: {Description}. Usuario: {UserId}", desc, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al agregar log. Usuario: {UserId}, Descripción: {Description}", 
                    userId, desc);
            }
        }

        public async Task AddLog(
            string desc,
            string tipoLog = "Info",
            string origen = "Sistema",
            string userId = "Sistema",
            string orgId = "Sistema",
            CancellationToken cancellationToken = default)
        {
            try
            {
                var log = new Z910_Log(
                    userId: userId,
                    orgId: orgId,
                    desc: desc,
                    tipoLog: tipoLog,
                    origen: origen
                );

                await _context.Log.AddAsync(log, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation(
                    "Log del sistema agregado - Tipo: {TipoLog}, Origen: {Origen}, Usuario: {UserId}, Descripción: {Description}", 
                    tipoLog, origen, userId, desc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error al agregar log del sistema. Tipo: {TipoLog}, Origen: {Origen}, Usuario: {UserId}, Descripción: {Description}", 
                    tipoLog, origen, userId, desc);
            }
        }

        Task<ApiRespAll<Z900_Bitacora>> IRepoBitacora.GetBitacoraFiltrada(
            string orgId,
            FiltroBitacora filtro,
            bool byPassCache = false,
            CancellationToken cancellationToken = default,
            ApplicationUser elUser = null)
        {
            return GetBitacoraFiltrada(orgId, filtro, byPassCache, cancellationToken, elUser);
        }
    }
} 