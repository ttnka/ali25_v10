using System.Collections.Concurrent;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Ali25_V10.Data.Sistema;

public class Repo<TEntity, TDataContext> : IRepo<TEntity>
    where TEntity : class 
    where TDataContext : DbContext
{ 
#region  servicios
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheOptions;
    private readonly SemaphoreSlim _lock = new(1, 1);
    protected readonly TDataContext _context;
    internal DbSet<TEntity> _dbset;
    private static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
    private readonly IRepoBitacora _repoBitacora;
    // private readonly IRepoLog _repoLog;  // Comentado IRepoLog
#endregion

#region  Caché
    // TEMPORAL - Diagnóstico de caché
    private static List<string> _cacheKeysHistory = new();
    private const int MAX_HISTORY = 10;

    // Constantes de caché
    private const int DEFAULT_CACHE_MINUTES = 30;
    private const int DEFAULT_SLIDING_MINUTES = 10;
    private string BorrarTEXTO = "";
    private class FilterValueExtractor : ExpressionVisitor
    {
        public Dictionary<string, object> ExtractedValues { get; } = new();

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.Left is MemberExpression memberExpr)
            {
                object? value = null;
                
                if (node.Right is ConstantExpression constExpr)
                {
                    value = constExpr.Value;
                }
                else if (node.Right is MemberExpression rightMemberExpr)
                {
                    value = Expression.Lambda(rightMemberExpr).Compile().DynamicInvoke();
                }

                if (value != null)
                {
                    ExtractedValues[memberExpr.Member.Name] = value;
                }
            }
            return base.VisitBinary(node);
        }
    }

    public Repo(
        TDataContext context,
        IMemoryCache cache,
        IConfiguration config,
        IRepoBitacora repoBitacora,
        IServiceScopeFactory serviceScopeFactory)
    {
        _context = context;
        _dbset = context.Set<TEntity>();
        _repoBitacora = repoBitacora;
        _cache = cache;
        _serviceScopeFactory = serviceScopeFactory;
        
        // Configuración del caché
        var cacheMinutes = config.GetValue("Cache:MinutesToLive", DEFAULT_CACHE_MINUTES);
        var slidingMinutes = config.GetValue("Cache:SlidingMinutes", DEFAULT_SLIDING_MINUTES);
        
        _cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(cacheMinutes))
            .SetSlidingExpiration(TimeSpan.FromMinutes(slidingMinutes))
            .SetSize(1)
            .SetPriority(CacheItemPriority.Normal);
    }

    private (string prefix, string cacheKey) GetCacheKey(
        string orgId,
        string entityName,
        ApplicationUser elUser,
        Expression<Func<TEntity, bool>> filtro = null,
        object regId = null)
    {
        try 
        {
            // Clave base
            var prefix = $"{orgId ?? "NoOrg"}:{entityName ?? "NoEntity"}";
            var cacheKey = prefix;

            // Extraer los filtros
            if (filtro != null)
            {
                var filterVisitor = new FilterValueExtractor();
                filterVisitor.Visit(filtro);
                
                // Ordenar los filtros por nombre de propiedad
                var filterParams = filterVisitor.ExtractedValues
                    .OrderBy(x => x.Key)
                    .Select(x => $"{x.Key}={x.Value}")
                    .ToList();

                if (filterParams.Any())
                {
                    cacheKey += $":filtros:{string.Join(",", filterParams)}";
                }
            }
            else
            {
                // Si no hay filtros, marcarlo explícitamente
                cacheKey += ":sin_filtros";
            }

            if (regId != null)
            {
                cacheKey += $":id:{regId}";
            }

            

            return (prefix, cacheKey);
        }
        catch (Exception ex)
        {
            _repoBitacora.AddLog(
                userId: elUser.UserId,
                orgId: elUser.OrgId,
                desc: $"Error en GetCacheKey: {ex.Message}",
                tipoLog: "Error",
                origen: "Repo.GetCacheKey"
            ).Wait();
            throw;
        }
    }

    // Para tipos referencia (clases)
    private async Task<(bool found, T? value)> TryGetFromCache<T>(string key) where T : class
    {
        await _lock.WaitAsync();
        try
        {
            if (_cache.TryGetValue(key, out object? cached) && cached is T value)
            {
                return (true, value);
            }
            return (false, null);
        }
        finally
        {
            _lock.Release();
        }
    }

    // Para tipos valor (int, etc)
    private async Task<(bool found, int value)> TryGetFromCacheInt(string key)
    {
        await _lock.WaitAsync();
        try
        {
            if (_cache.TryGetValue(key, out object? cached) && cached is int value)
            {
                return (true, value);
            }
            return (false, 0);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task SetInCache<T>(string key, T value)
    {
        await _lock.WaitAsync();
        try
        {
            _cache.Set(key, value, _cacheOptions);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task InvalidateCache(string orgId)
    {
        await _lock.WaitAsync();
        try
        {
            var pattern = $"{typeof(TEntity).Name}_{orgId}";
            // Usar reflection para obtener las claves del cache
            var field = typeof(MemoryCache).GetField("_entries", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var entries = field?.GetValue(_cache) as IDictionary<string, ICacheEntry>;
            
            if (entries != null)
            {
                foreach (var key in entries.Keys.Where(k => k.StartsWith(pattern)))
                {
                    _cache.Remove(key);
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    #endregion

    //GET
    public async Task<ApiRespAll<TEntity>> Get(
        string orgId,
        ApplicationUser elUser,
        Expression<Func<TEntity, bool>> filtro = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderby = null,
        string propiedades = "",
        bool byPassCache = false,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(orgId, typeof(TEntity).Name, elUser, filtro);
        BorrarTEXTO += cacheKey.cacheKey + " _ ";
        if (!byPassCache)
        {
            var (found, cached) = await TryGetFromCache<List<TEntity>>(cacheKey.cacheKey);
            if (found && cached != null)
            {
                return new ApiRespAll<TEntity> { Exito = true, DataVarios = cached };
            }
        }

        try
        {
            IQueryable<TEntity> query = _dbset;
            if (filtro != null)
            {
                query = query.Where(filtro);
            }

            if (orderby != null)
            {
                query = orderby(query);
            }

            var respuesta = new ApiRespAll<TEntity> { Exito = false };
            respuesta.DataVarios = await query.ToListAsync(cancellationToken);
            respuesta.Exito = true;

            if (!byPassCache && orgId != "Vacio")
            {
                await SetInCache(cacheKey.cacheKey, respuesta.DataVarios);
            }

            return respuesta;
        }
        catch (Exception ex)
        {
            await _repoBitacora.AddLog(
                userId: elUser?.UserId ?? "Sistema",
                orgId: orgId,
                desc: $"Error en Get: {ex.Message}",
                tipoLog: "Error",
                origen: $"Repo.Get<{typeof(TEntity).Name}>"
            );
            return new ApiRespAll<TEntity> { Exito = false, MsnError = new List<string> { ex.Message } };
        }
    }

    //GetAll
    public async Task<ApiRespAll<TEntity>> GetAll(
        ApplicationUser elUser,
        bool byPassCache = false,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(elUser.OrgId, typeof(TEntity).Name, elUser);

        if (!byPassCache)
        {
            var (found, cached) = await TryGetFromCache<List<TEntity>>(cacheKey.cacheKey);
            if (found && cached != null)
            {
                return new ApiRespAll<TEntity> { Exito = true, DataVarios = cached };
            }
        }

        try
        {
            var respuesta = new ApiRespAll<TEntity> { Exito = false };
            respuesta.DataVarios = await _dbset.ToListAsync(cancellationToken);
            respuesta.Exito = true;

            if (!byPassCache && elUser.OrgId != "Vacio")
            {
                await SetInCache(cacheKey.cacheKey, respuesta.DataVarios);
            }

            return respuesta;
        }
        catch (Exception ex)
        {
            await _repoBitacora.AddLog(
                userId: elUser.UserId,
                orgId: elUser.OrgId,
                desc: $"Error en GetAll: {ex.Message}",
                tipoLog: "Error",
                origen: $"Repo.GetAll<{typeof(TEntity).Name}>"
            );
            return new ApiRespAll<TEntity> { Exito = false, MsnError = new List<string> { ex.Message } };
        }
    }

    //GetById
    public async Task<ApiRespAll<TEntity>> GetById(
        object id,
        string orgId,
        ApplicationUser elUser,
        bool byPassCache = false,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(orgId, typeof(TEntity).Name, elUser, null, id);

        if (!byPassCache)
        {
            var (found, cached) = await TryGetFromCache<TEntity>(cacheKey.cacheKey);
            if (found && cached != null)
            {
                return new ApiRespAll<TEntity> { Exito = true, DataUno = cached };
            }
        }

        try
        {
            var respuesta = new ApiRespAll<TEntity> { Exito = false };
            var entity = await _dbset.FindAsync(new[] { id }, cancellationToken);
            if (entity != null)
            {
                respuesta.DataUno = entity;
                respuesta.Exito = true;

                if (!byPassCache)
                {
                    await SetInCache(cacheKey.cacheKey, entity);
                }
            }
            return respuesta;
        }
        catch (Exception ex)
        {
            await _repoBitacora.AddLog(
                userId: elUser?.UserId ?? "Sistema",
                orgId: orgId,
                desc: $"Error en GetById: {ex.Message}",
                tipoLog: "Error",
                origen: $"Repo.GetById<{typeof(TEntity).Name}>"
            );
            return new ApiRespAll<TEntity> { Exito = false, MsnError = new List<string> { ex.Message } };
        }
    }

    //Insert
    public async Task<ApiRespAll<TEntity>> Insert(
        TEntity entity,
        string orgId,
        ApplicationUser elUser,
        CancellationToken cancellationToken = default)
    {
        var respuesta = new ApiRespAll<TEntity> { Exito = false };
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            await _dbset.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            
            // Invalidar caché
            await InvalidateCache(orgId);
            
            respuesta.Exito = true;
            respuesta.DataUno = entity;
            
            await _repoBitacora.AddBitacora(
                userId: elUser?.UserId ?? "Sistema",
                desc: $"Se insertó nuevo {typeof(TEntity).Name}",
                orgId: orgId
            );
            
            return respuesta;
        }
        catch (Exception ex)
        {
            await _repoBitacora.AddLog(
                userId: elUser?.UserId ?? "Sistema",
                orgId: orgId,
                desc: $"Error en Insert: {ex.Message}",
                tipoLog: "Error",
                origen: $"Repo.Insert<{typeof(TEntity).Name}>"
            );
            respuesta.MsnError.Add(ex.Message);
            return respuesta;
        }
        finally
        {
            semaphore.Release();
        }
    }

    //InsertPlus
    public async Task<ApiRespAll<TEntity>> InsertPlus(
        IEnumerable<TEntity> entities,
        string orgId,
        ApplicationUser elUser,
        CancellationToken cancellationToken = default)
    {
        var respuesta = new ApiRespAll<TEntity> { Exito = false };
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            if (entities == null || !entities.Any())
            {
                respuesta.MsnError.Add("No hay entidades para insertar");
                return respuesta;
            }

            await _dbset.AddRangeAsync(entities, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            
            // Invalidar caché
            await InvalidateCache(orgId);
            
            respuesta.Exito = true;
            respuesta.DataVarios = entities.ToList();
            
            await _repoBitacora.AddBitacora(
                userId: elUser?.UserId ?? "Sistema",
                desc: $"Se insertaron {entities.Count()} {typeof(TEntity).Name}",
                orgId: orgId
            );
            
            return respuesta;
        }
        catch (Exception ex)
        {
            await _repoBitacora.AddLog(
                userId: elUser?.UserId ?? "Sistema",
                orgId: orgId,
                desc: $"Error en InsertPlus: {ex.Message}",
                tipoLog: "Error",
                origen: $"Repo.InsertPlus<{typeof(TEntity).Name}>"
            );
            respuesta.MsnError.Add(ex.Message);
            return respuesta;
        }
        finally
        {
            semaphore.Release();
        }
    }
    
    //Update
    public virtual async Task<ApiRespAll<TEntity>> Update(
        TEntity entityToUpdate,
        string orgId,
        ApplicationUser elUser,
        CancellationToken cancellationToken = default)
    {
        var respuesta = new ApiRespAll<TEntity> { Exito = false };
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            _dbset.Update(entityToUpdate);
            await _context.SaveChangesAsync(cancellationToken);
            
            // Invalidar caché
            await InvalidateCache(orgId);
            
            respuesta.Exito = true;
            respuesta.DataUno = entityToUpdate;
            
            await _repoBitacora.AddBitacora(
                userId: elUser?.UserId ?? "Sistema",
                desc: $"Se actualizó {typeof(TEntity).Name}",
                orgId: orgId
            );
            
            return respuesta;
        }
        catch (Exception ex)
        {
            await _repoBitacora.AddLog(
                userId: elUser?.UserId ?? "Sistema",
                orgId: orgId,
                desc: $"Error en Update: {ex.Message}",
                tipoLog: "Error",
                origen: $"Repo.Update<{typeof(TEntity).Name}>"
            );
            respuesta.MsnError.Add(ex.Message);
            return respuesta;
        }
        finally
        {
            semaphore.Release();
        }
    }

    //GetUserId
    public async Task<ApplicationUser?> GetUserById(string id)
    {
        await semaphore.WaitAsync();
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await dbContext.Users.FindAsync(id);
        }
        catch (Exception ex)
        {
            await _repoBitacora.AddLog(
                userId: "Sistema",
                orgId: "Sistema",
                desc: $"Error en GetUserById: {ex.Message}",
                tipoLog: "Error",
                origen: $"Repo.GetUserById"
            );
            return null;
        }
        finally
        {
            semaphore.Release();
        }
    }


    //UpdateMisDatos
    public virtual async Task<ApiRespAll<TEntity>> UpdateMisDatos(TEntity entityToUpdate,
            string orgId, ApplicationUser elUser, CancellationToken cancellationToken = default)
    {
        var respuesta = new ApiRespAll<TEntity> { Exito = false, Varios = false };
        
        if (entityToUpdate == null)
        {
            await _repoBitacora.AddBitacora(
                userId: elUser.UserId,
                desc: $"Intento de actualizar MisDatos con entidad nula en {typeof(TEntity).Name}",
                orgId: orgId
            );
            respuesta.MsnError.Add("No se proporcionó una entidad para actualizar.");
            return respuesta;
        }

        var cacheKey = GetCacheKey(orgId, typeof(TEntity).Name, elUser, null, null);
        await semaphore.WaitAsync(cancellationToken);
        
        try
        {
            var dbSet = _context.Set<TEntity>();
            dbSet.Attach(entityToUpdate);
            _context.Entry(entityToUpdate).State = EntityState.Modified;
            
            int affectedRows = await _context.SaveChangesAsync(cancellationToken);
            
            if (affectedRows > 0)
            {
                respuesta.Exito = true;
                respuesta.DataUno = entityToUpdate;
                respuesta.Texto = "Los datos se actualizaron correctamente.";
                
                await _repoBitacora.AddBitacora(
                    userId: elUser.UserId,
                    desc: $"Se actualizaron correctamente MisDatos en {typeof(TEntity).Name}",
                    orgId: orgId
                );

                UpdateLastUpdateTime(cacheKey.cacheKey, false, elUser);
            }
            else
            {
                await _repoBitacora.AddBitacora(
                    userId: elUser.UserId,
                    desc: $"No se realizaron cambios en MisDatos de {typeof(TEntity).Name}",
                    orgId: orgId
                );
                respuesta.MsnError.Add("No se realizaron cambios en los datos.");
            }
        }
        catch (DbUpdateConcurrencyException ex)
        {
            string exceptionDetails = $"Error de concurrencia al actualizar MisDatos en {typeof(TEntity).Name}: ";
            exceptionDetails += $"Message: {ex.Message}\nStackTrace: {ex.StackTrace}\nData:\n";
            foreach (var key in ex.Data.Keys)
            {
                var value = ex.Data[key];
                exceptionDetails += $"{key}: {value}\n";
            }
            
            await _repoBitacora.AddLog(
                userId: elUser?.UserId ?? "Sistema",
                orgId: orgId ?? "Sistema",
                desc: exceptionDetails,
                tipoLog: "Error",
                origen: $"Repo.{nameof(UpdateMisDatos)}<{typeof(TEntity).Name}>"
            );
            respuesta.MsnError.Add("Error de concurrencia al actualizar los datos. Los datos han sido modificados por otro usuario.");
        }
        catch (DbUpdateException ex)
        {
            string exceptionDetails = $"Error de base de datos al actualizar MisDatos en {typeof(TEntity).Name}: ";
            exceptionDetails += $"Message: {ex.Message}\nStackTrace: {ex.StackTrace}\nData:\n";
            foreach (var key in ex.Data.Keys)
            {
                var value = ex.Data[key];
                exceptionDetails += $"{key}: {value}\n";
            }
            
            await _repoBitacora.AddLog(
                userId: elUser?.UserId ?? "Sistema",
                orgId: orgId ?? "Sistema",
                desc: exceptionDetails,
                tipoLog: "Error",
                origen: $"Repo.{nameof(UpdateMisDatos)}<{typeof(TEntity).Name}>"
            );
            respuesta.MsnError.Add($"Error al guardar los cambios en la base de datos: {ex.InnerException?.Message ?? ex.Message}");
        }
        catch (Exception ex)
        {
            string exceptionDetails = $"Error inesperado al actualizar MisDatos en {typeof(TEntity).Name}: ";
            exceptionDetails += $"Message: {ex.Message}\nStackTrace: {ex.StackTrace}\nData:\n";
            foreach (var key in ex.Data.Keys)
            {
                var value = ex.Data[key];
                exceptionDetails += $"{key}: {value}\n";
            }
            
            await _repoBitacora.AddLog(
                userId: elUser?.UserId ?? "Sistema",
                orgId: orgId ?? "Sistema",
                desc: exceptionDetails,
                tipoLog: "Error",
                origen: $"Repo.{nameof(UpdateMisDatos)}<{typeof(TEntity).Name}>"
            );
            respuesta.MsnError.Add($"Error inesperado al actualizar los datos: {ex.Message}");
        }
        finally
        {
            semaphore.Release();
        }
        
        return respuesta;
    }
    
    //UpdatePlus
    public async Task<ApiRespAll<TEntity>> UpdatePlus(
        IEnumerable<TEntity> entities,
        string orgId,
        ApplicationUser elUser,
        CancellationToken cancellationToken = default)
    {
        var respuesta = new ApiRespAll<TEntity> { Exito = false };
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            if (entities == null || !entities.Any())
            {
                respuesta.MsnError.Add("No hay entidades para actualizar");
                return respuesta;
            }

            _dbset.UpdateRange(entities);
            await _context.SaveChangesAsync(cancellationToken);
            
            // Invalidar caché
            await InvalidateCache(orgId);
            
            respuesta.Exito = true;
            respuesta.DataVarios = entities.ToList();
            
            await _repoBitacora.AddBitacora(
                userId: elUser?.UserId ?? "Sistema",
                desc: $"Se actualizaron {entities.Count()} {typeof(TEntity).Name}",
                orgId: orgId
            );
            
            return respuesta;
        }
        catch (Exception ex)
        {
            await _repoBitacora.AddLog(
                userId: elUser?.UserId ?? "Sistema",
                orgId: orgId,
                desc: $"Error en UpdatePlus: {ex.Message}",
                tipoLog: "Error",
                origen: $"Repo.UpdatePlus<{typeof(TEntity).Name}>"
            );
            respuesta.MsnError.Add(ex.Message);
            return respuesta;
        }
        finally
        {
            semaphore.Release();
        }
    }

    //DeletePLus

    //GetCount
    public async Task<int> GetCount(
        string orgId,
        ApplicationUser elUser,
        Expression<Func<TEntity, bool>> filtro = null,
        bool byPassCache = false,
        CancellationToken cancellationToken = default)
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            var parameters = new Dictionary<string, object>();
            if (filtro != null)
            {
                var extractor = new FilterValueExtractor();
                extractor.Visit(filtro);
                foreach (var param in extractor.ExtractedValues)
                {
                    parameters[param.Key] = param.Value;
                }
            }

            var cacheKey = $"count_{GetCacheKey(orgId, typeof(TEntity).Name, elUser, filtro, null).cacheKey}";

            if (!byPassCache)
            {
                var (found, cached) = await TryGetFromCacheInt(cacheKey);
                if (found)
                {
                    return cached;
                }
            }

            try
            {
                IQueryable<TEntity> query = _dbset;
                if (filtro != null)
                {
                    query = query.Where(filtro);
                }

                var count = await query.CountAsync(cancellationToken);

                if (!byPassCache)
                {
                    await SetInCache(cacheKey, count);
                }

                return count;
            }
            catch (Exception ex)
            {
                await _repoBitacora.AddLog(
                    userId: elUser?.UserId ?? "Sistema",
                    orgId: orgId,
                    desc: $"Error en GetCount: {ex.Message}",
                    tipoLog: "Error",
                    origen: $"Repo.GetCount<{typeof(TEntity).Name}>"
                );
                return 0;
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<bool> DeleteEntity(
        TEntity entityToDel,
        ApplicationUser elUser)
    {
        await semaphore.WaitAsync();
        try
        {
            if (entityToDel == null)
            {
                await _repoBitacora.AddLog(
                    userId: elUser.UserId,
                    orgId: elUser.OrgId,
                    desc: "Intento de eliminar entidad nula",
                    tipoLog: "Error",
                    origen: $"Repo.DeleteEntity<{typeof(TEntity).Name}>"
                );
                return false;
            }

            _dbset.Remove(entityToDel);
            await _context.SaveChangesAsync();
            
            // Invalidar caché
            await InvalidateCache(elUser.OrgId);
            
            await _repoBitacora.AddBitacora(
                userId: elUser.UserId,
                desc: $"Se eliminó {typeof(TEntity).Name}",
                orgId: elUser.OrgId
            );
            
            return true;
        }
        catch (Exception ex)
        {
            await _repoBitacora.AddLog(
                userId: elUser.UserId,
                orgId: elUser.OrgId,
                desc: $"Error en DeleteEntity: {ex.Message}",
                tipoLog: "Error",
                origen: $"Repo.DeleteEntity<{typeof(TEntity).Name}>"
            );
            return false;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public virtual async Task<bool> Delete(
        TEntity entity, 
        string orgId,
        CancellationToken cancellationToken = default, 
        ApplicationUser elUser = null)
    {
        if (entity == null)
        {
            await _repoBitacora.AddBitacora(
                userId: elUser.UserId,
                desc: "Intento de eliminar entidad nula",
                orgId: orgId
            );
            return false;
        }

        await semaphore.WaitAsync(cancellationToken);
        try
        {
            _dbset.Remove(entity);
            await _context.SaveChangesAsync();
            
            await _repoBitacora.AddBitacora(
                userId: elUser.UserId,
                desc: $"Se eliminó correctamente {typeof(TEntity).Name}",
                orgId: orgId
            );

            var cacheKey = GetCacheKey(orgId, typeof(TEntity).Name, elUser, null, null);
            UpdateLastUpdateTime(cacheKey.cacheKey, false, elUser);
            return true;
        }
        catch (Exception ex)
        {
            string exceptionDetails = $"Error al eliminar {typeof(TEntity).Name}: ";
            exceptionDetails += $"Message: {ex.Message}\nStackTrace: {ex.StackTrace}\nData:\n";
            foreach (var key in ex.Data.Keys)
            {
                var value = ex.Data[key];
                exceptionDetails += $"{key}: {value}\n";
            }
            
            await _repoBitacora.AddLog(
                userId: elUser?.UserId ?? "Sistema",
                orgId: orgId,
                desc: exceptionDetails,
                tipoLog: "Error",
                origen: $"Repo.{nameof(Delete)}<{typeof(TEntity).Name}>"
            );
            return false;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<bool> DeleteEntity(object id, string orgId, 
            CancellationToken cancellationToken = default, ApplicationUser elUser = null)
    {
        if (id == null)
        {
            await _repoBitacora.AddBitacora(
                userId: elUser.UserId,
                desc: $"Intento de eliminar {typeof(TEntity).Name} con ID nulo",
                orgId: orgId
            );
            return false;
        }

        var cacheKey = GetCacheKey(orgId, typeof(TEntity).Name, elUser, null, id);
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            var entityToDel = await _dbset.FindAsync(new[] { id }, cancellationToken);
            if (entityToDel == null)
            {
                await _repoBitacora.AddBitacora(
                    userId: elUser.UserId,
                    desc: $"Intento de eliminar {typeof(TEntity).Name} inexistente con ID: {id}",
                    orgId: orgId
                );
                return false;
            }

            _dbset.Remove(entityToDel);
            int result = await _context.SaveChangesAsync(cancellationToken);

            if (result > 0)
            {
                await _repoBitacora.AddBitacora(
                    userId: elUser.UserId,
                    desc: $"Se eliminó correctamente {typeof(TEntity).Name} con ID: {id}",
                    orgId: orgId
                );
                UpdateLastUpdateTime(cacheKey.cacheKey, false, elUser);
                return true;
            }

            await _repoBitacora.AddBitacora(
                userId: elUser.UserId,
                desc: $"No se pudo eliminar {typeof(TEntity).Name} con ID: {id}",
                orgId: orgId
            );
            return false;
        }
        catch (Exception ex)
        {
            string exceptionDetails = $"Error al eliminar {typeof(TEntity).Name} con ID {id}: ";
            exceptionDetails += $"Message: {ex.Message}\nStackTrace: {ex.StackTrace}\nData:\n";
            foreach (var key in ex.Data.Keys)
            {
                var value = ex.Data[key];
                exceptionDetails += $"{key}: {value}\n";
            }
            
            await _repoBitacora.AddLog(
                userId: elUser?.UserId ?? "Sistema",
                orgId: orgId ?? "Sistema",
                desc: exceptionDetails,
                tipoLog: "Error",
                origen: $"Repo.{nameof(DeleteEntity)}<{typeof(TEntity).Name}>"
            );
            return false;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private void UpdateLastUpdateTime(string cacheKey, bool consulta, ApplicationUser elUser)
    {
        try
        {
            var now = DateTime.UtcNow;
            if (!consulta)
            {
                _cache.Remove(cacheKey);
            }
        }
        catch (Exception ex)
        {
            _repoBitacora.AddLog(
                userId: elUser.UserId,
                orgId: elUser.OrgId,
                desc: $"Error en UpdateLastUpdateTime: {ex.Message}",
                tipoLog: "Error",
                origen: "Repo.UpdateLastUpdateTime"
            ).Wait();
        }
    }
}

