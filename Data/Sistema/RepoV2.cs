using System.Collections.Concurrent;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;

namespace Ali25_V10.Data.Sistema;

public class RepoV2<TEntity, TDataContext> : IRepo<TEntity>
    where TEntity : class 
    where TDataContext : DbContext
{ 
#region  servicios
    private readonly IServiceScopeFactory _serviceScopeFactory;
    protected readonly TDataContext context;
    internal DbSet<TEntity> dbset;
    private readonly ApplicationDbContext _appDbContext;
    private static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
    private readonly ILogger<Repo<TEntity, TDataContext>> _logger;
    private readonly IRepoBitacora _repoBitacora;
    // private readonly IRepoLog _repoLog;  // Comentado IRepoLog
#endregion

    // TEMPORAL - Diagnóstico de caché
    private static List<string> _cacheKeysHistory = new();
    private const int MAX_HISTORY = 10;

    public RepoV2(TDataContext dataContext, IServiceScopeFactory serviceScopeFactory,
            ApplicationDbContext appDbContext,
            ILogger<Repo<TEntity, TDataContext>> logger,
            IRepoBitacora repoBitacora)
    {
        _serviceScopeFactory = serviceScopeFactory;
        context = dataContext;
        dbset = context.Set<TEntity>();
        _appDbContext = appDbContext;
        _logger = logger;
        _repoBitacora = repoBitacora;
        // _repoLog = repoLog;  // Comentado si existe en el constructor
    }
#region Cache
    private int Min_actualizar = 30;        
    private static ConcurrentDictionary<string, (object Data, DateTime CacheTime, TimeSpan ExpirationTime)> _globalCache = 
            new ConcurrentDictionary<string, (object, DateTime, TimeSpan)>();
    private static ConcurrentDictionary<string, DateTime> _lastUpdateTime = new ConcurrentDictionary<string, DateTime>();
    private (string prefix, string cacheKey) GetCacheKey(string orgId, string entityName, 
        Expression<Func<TEntity, bool>> filtro = null, object regId = null)
    {
        try 
        {
            var prefix = $"{orgId ?? "NoOrg"}:{entityName ?? "NoEntity"}:";
            var cacheKey = prefix;

            if (filtro != null)
            {
                var filterString = filtro.ToString().Replace(" ", "").Replace("\r\n", "");
                cacheKey += $"filtro:{filterString}:";
            }

            if (regId != null)
            {
                cacheKey += $"id:{regId}";
            }

            return (prefix, cacheKey);
        }
        catch (Exception ex)
        {
            _repoBitacora.AddLog(
                userId: "Sistema",
                orgId: orgId,
                desc: $"Error en GetCacheKey: {ex.Message}\nStackTrace: {ex.StackTrace}",
                tipoLog: "Error",
                origen: $"Repo.GetCacheKey<{typeof(TEntity).Name}>"
            ).Wait();
            throw;
        }
    }
    private class FilterValueExtractor : ExpressionVisitor
    {
        public Dictionary<string, object> ExtractedValues { get; } = new Dictionary<string, object>();

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Equal || 
                node.NodeType == ExpressionType.NotEqual ||
                node.NodeType == ExpressionType.GreaterThan ||
                node.NodeType == ExpressionType.GreaterThanOrEqual ||
                node.NodeType == ExpressionType.LessThan ||
                node.NodeType == ExpressionType.LessThanOrEqual)
            {
                if (node.Left is MemberExpression memberExpr)
                {
                    if (node.Right is ConstantExpression constantExpr)
                    {
                        ExtractedValues[memberExpr.Member.Name] = constantExpr.Value;
                    }
                    else if (node.Right is MemberExpression rightMemberExpr)
                    {
                        var rightMember = Expression.Lambda(rightMemberExpr).Compile().DynamicInvoke();
                        ExtractedValues[memberExpr.Member.Name] = rightMember;
                    }
                }
            }
            return base.VisitBinary(node);
        }
    }
    
    private void UpdateLastUpdateTime(string cacheKey, bool consulta)
    {
        try
        {
            var now = DateTime.UtcNow;
            if (!consulta)
            {
                var prefix = cacheKey.Split(':')[0] + ":" + cacheKey.Split(':')[1] + ":";
                var keysToRemove = _globalCache.Keys
                    .Where(k => k.StartsWith(prefix))
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _globalCache.TryRemove(key, out _);
                }
            }
            _lastUpdateTime[cacheKey] = now;
        }
        catch (Exception ex)
        {
            _repoBitacora.AddLog(
                userId: "Sistema",
                orgId: cacheKey.Split(':')[0],
                desc: $"Error en UpdateLastUpdateTime: {ex.Message}\nStackTrace: {ex.StackTrace}\nCacheKey: {cacheKey}, Consulta: {consulta}",
                tipoLog: "Error",
                origen: $"Repo.UpdateLastUpdateTime<{typeof(TEntity).Name}>"
            ).Wait();
            throw;
        }
    }
    
    private bool IsCacheValid(string cacheKey, DateTime cacheTime, TimeSpan horaCaduca)
    {
        try
        {
            if(_lastUpdateTime.TryGetValue(cacheKey, out var lastUpdate) && lastUpdate > cacheTime)
            {
                return false;
            }
            return _globalCache.ContainsKey(cacheKey) && (DateTime.UtcNow - cacheTime) < horaCaduca;
        }
        catch (Exception ex)
        {
            _repoBitacora.AddLog(
                userId: "Sistema",
                orgId: cacheKey.Split(':')[0],
                desc: $"Error en IsCacheValid: {ex.Message}\nStackTrace: {ex.StackTrace}\nCacheKey: {cacheKey}, CacheTime: {cacheTime}",
                tipoLog: "Error",
                origen: $"Repo.IsCacheValid<{typeof(TEntity).Name}>"
            ).Wait();
            throw;
        }
    }
    private string ElPrefix(string orgId, string tentity) => $"{orgId}:{tentity}";
    
/*
private int Min_actualizar = 30;        
    private static ConcurrentDictionary<string, (object Data, DateTime CacheTime, TimeSpan ExpirationTime)> _globalCache = 
            new ConcurrentDictionary<string, (object, DateTime, TimeSpan)>();
    private static ConcurrentDictionary<string, DateTime> _lastUpdateTime = new ConcurrentDictionary<string, DateTime>();
    private (string prefix, string cacheKey) GetCacheKey(string orgId, string entityName, 
        Expression<Func<TEntity, bool>> filtro = null, object regId = null)
    {
        var prefix = $"{orgId}:{entityName}:";
        var cacheKey = prefix;

        if (filtro != null)
        {
            // Convertir la expresión del filtro a una representación de string más confiable
            var filterString = filtro.ToString().Replace(" ", "").Replace("\r\n", "");
            cacheKey += $"filtro:{filterString}:";
        }

        if (regId != null)
        {
            cacheKey += $"id:{regId}";
        }

        return (prefix, cacheKey);
    }
    private class FilterValueExtractor : ExpressionVisitor
    {
        public Dictionary<string, object> ExtractedValues { get; } = new Dictionary<string, object>();

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Equal || 
                node.NodeType == ExpressionType.NotEqual ||
                node.NodeType == ExpressionType.GreaterThan ||
                node.NodeType == ExpressionType.GreaterThanOrEqual ||
                node.NodeType == ExpressionType.LessThan ||
                node.NodeType == ExpressionType.LessThanOrEqual)
            {
                if (node.Left is MemberExpression memberExpr)
                {
                    if (node.Right is ConstantExpression constantExpr)
                    {
                        ExtractedValues[memberExpr.Member.Name] = constantExpr.Value;
                    }
                    else if (node.Right is MemberExpression rightMemberExpr)
                    {
                        var rightMember = Expression.Lambda(rightMemberExpr).Compile().DynamicInvoke();
                        ExtractedValues[memberExpr.Member.Name] = rightMember;
                    }
                }
            }
            return base.VisitBinary(node);
        }
    }
    
    private void UpdateLastUpdateTime(string cacheKey, bool consulta)
    {
        var now = DateTime.UtcNow;
        if (!consulta)
        {
            var prefix = cacheKey.Substring(0, cacheKey.IndexOf(':', cacheKey.IndexOf(':')+1)+1);
            var regsEliminar = _globalCache.Keys.Where(x => x.StartsWith(prefix)).ToList();
            foreach (var key in regsEliminar)
            {
                _globalCache.TryRemove(key, out _);
            }
        }
        _lastUpdateTime[cacheKey] = now;
    }
    
    private bool IsCacheValid(string cacheKey, DateTime cacheTime, TimeSpan horaCaduca)
    {
        if(_lastUpdateTime.TryGetValue(cacheKey, out var lastUpdate) && lastUpdate > cacheTime)
        {
            return false;
        }
        return _globalCache.ContainsKey(cacheKey) && (DateTime.UtcNow - cacheTime) < horaCaduca;
    }
    private string ElPrefix(string orgId, string tentity) => $"{orgId}:{tentity}";
    
*/
#endregion

    //GET
    public async Task<ApiRespAll<TEntity>> Get(string orgId,
        Expression<Func<TEntity, bool>> filtro = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderby = null,
        string propiedades = "", bool byPassCache = false,
        CancellationToken cancellationToken = default,
        ApplicationUser elUser = null)
    {
        var respuesta = new ApiRespAll<TEntity> { Exito = false, Varios = true };
        
        try 
        {
            ValidateUser(elUser);
            await semaphore.WaitAsync(cancellationToken);

            // BORRAR DESPUÉS - Log para debug
            await _repoBitacora.AddLog(
                userId: elUser?.Id ?? "Sistema",
                orgId: orgId,
                desc: $"DEBUG: Get - Tipo={typeof(TEntity).Name}, " +
                      $"OrgId={orgId}, BypassCache={byPassCache}, " +
                      $"Filtro={filtro?.ToString()}",
                tipoLog: "Debug",
                origen: "Repo.Get"
            );

            // Verificar caché
            var (prefix, cacheKey) = GetCacheKey(orgId, typeof(TEntity).Name, filtro);

            await _repoBitacora.AddBitacora("Temp", $"va {prefix}, {cacheKey}", "temp1");

            // TEMPORAL - Registro de diagnóstico de caché
            _cacheKeysHistory.Add($"Time: {DateTime.Now}, Key: {cacheKey}, Hit: {_globalCache.ContainsKey(cacheKey)}");
            if (_cacheKeysHistory.Count > MAX_HISTORY)
                _cacheKeysHistory.RemoveAt(0);

            // TEMPORAL - Log de diagnóstico
            await _repoBitacora.AddLog(
                userId: elUser?.Id ?? "Sistema",
                orgId: orgId,
                desc: $"Cache Keys History:\n{string.Join("\n", _cacheKeysHistory)}",
                tipoLog: "Debug",
                origen: "Repo.Get.CacheDebug"
            );

            if (!byPassCache && _globalCache.TryGetValue(cacheKey, out var cachedResultado))
            {
                var (cachedRespuesta, cacheTime, horaCaduca) = ((ApiRespAll<TEntity>, DateTime, TimeSpan))cachedResultado;
                if (IsCacheValid(cacheKey, cacheTime, horaCaduca))
                {
                    // BORRAR DESPUÉS - Log para debug
                    await _repoBitacora.AddLog(
                        userId: elUser?.Id ?? "Sistema",
                        orgId: orgId,
                        desc: $"DEBUG: Cache hit - Key={cacheKey}",
                        tipoLog: "Debug",
                        origen: "Repo.Get"
                    );
                    return cachedRespuesta;
                }
            }

            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<TDataContext>();
                    IQueryable<TEntity> query = dbContext.Set<TEntity>().AsNoTracking();

                    if (filtro != null)
                    {
                        query = query.Where(filtro);
                    }

                    var resultado = await query.ToListAsync(cancellationToken);
                    respuesta.DataVarios = resultado ?? new List<TEntity>();
                    
                    if (resultado == null || !resultado.Any())
                    {
                        respuesta.Texto = "No se encontraron resultados.";
                    }
                    else if (orgId != "Vacio") 
                    {
                        _globalCache[cacheKey] = (respuesta, DateTime.UtcNow, TimeSpan.FromMinutes(Min_actualizar));
                        UpdateLastUpdateTime(prefix, true);
                    }
                    
                    respuesta.Exito = true;
                }    
            }
            finally
            {
                semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            respuesta.MsnError.Add($"Error al intentar leer el registro: {ex.Message}");
            respuesta.Exito = false;
        }

        return respuesta;
    }

    //GetAll
    public async Task<ApiRespAll<TEntity>> GetAll(ApplicationUser elUser, bool byPassCache = false,
        CancellationToken cancellationToken = default)
    {
        var respuesta = new ApiRespAll<TEntity> { Exito = false, Varios = true };
        
        try 
        {
            ValidateUser(elUser);
            
            // Verificar caché
            var (prefix, cacheKey) = GetCacheKey(elUser.OrgId, typeof(TEntity).Name);
            if (!byPassCache && _globalCache.TryGetValue(cacheKey, out var cachedResultado))
            {
                var (cachedRespuesta, cacheTime, horaCaduca) = ((ApiRespAll<TEntity>, DateTime, TimeSpan)) cachedResultado;
                if (IsCacheValid(cacheKey, cacheTime, horaCaduca))
                {
                    return cachedRespuesta;
                }
            }

            await semaphore.WaitAsync(cancellationToken);
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<TDataContext>();
                    var resultado = await dbContext.Set<TEntity>().AsNoTracking().ToListAsync(cancellationToken);

                    respuesta.DataVarios = resultado ?? new List<TEntity>();

                    if (resultado == null || !resultado.Any())
                    {
                        respuesta.Texto = "No se encontraron resultados.";
                    }
                    else 
                    {
                        _globalCache[cacheKey] = (respuesta, DateTime.UtcNow, TimeSpan.FromMinutes(Min_actualizar));
                        UpdateLastUpdateTime(prefix, true);
                    }

                    respuesta.Exito = true;
                }
            }
            finally
            {
                semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            respuesta.MsnError.Add($"Error al intentar leer los registros: {ex.Message}");
        }
        
        return respuesta;
    }      
    
    //GetById
    public virtual async Task<ApiRespAll<TEntity>> GetById(object id, string orgId, 
            bool byPassCache = false, CancellationToken cancellationToken = default, ApplicationUser elUser = null)
    {   
        var respuesta = new ApiRespAll<TEntity> { Exito = false, Varios = true };
        
        ValidateUser(elUser);

        // 1. PRIMERO verificar caché
        var (prefix, cacheKey) = GetCacheKey(orgId, typeof(TEntity).Name, null, id);
        if (!byPassCache && _globalCache.TryGetValue(cacheKey, out var cachedResultado))
        {
            var (cachedRespuesta, cacheTime, horaCaduca) = ((ApiRespAll<TEntity>, DateTime, TimeSpan))cachedResultado;
            if (IsCacheValid(cacheKey, cacheTime, horaCaduca))
            {
                await _repoBitacora.AddBitacora(
                    userId: elUser.UserId,
                    desc: $"GetById: Datos obtenidos desde caché para {typeof(TEntity).Name} id:{id}",
                    orgId: orgId
                );
                return cachedRespuesta;
            }
        }

        if (id == null)
        {
            await _repoBitacora.AddBitacora(
                userId: elUser.UserId,
                desc: $"Intento de obtener {typeof(TEntity).Name} con ID nulo",
                orgId: orgId
            );
            respuesta.MsnError.Add("El id proporcionado es nulo.");
            return respuesta;
        }

        await semaphore.WaitAsync(cancellationToken);
        try
        {
            var entity = await dbset.FindAsync(new[] { id }, cancellationToken);
            
            if (entity != null)
            {
                respuesta.DataUno = entity;
                
                // Solo guardar en caché si encontramos el registro
                if (orgId != "Vacio")
                {
                    _globalCache[cacheKey] = (respuesta, DateTime.UtcNow, TimeSpan.FromMinutes(Min_actualizar));
                    UpdateLastUpdateTime(prefix, true);
                }

                await _repoBitacora.AddBitacora(
                    userId: elUser.UserId,
                    desc: $"Registro {typeof(TEntity).Name} con ID {id} obtenido exitosamente",
                    orgId: orgId
                );
            }
            else
            {
                respuesta.Texto = $"No se encontró el registro con ID: {id}";
                await _repoBitacora.AddBitacora(
                    userId: elUser.UserId,
                    desc: $"No se encontró {typeof(TEntity).Name} con ID {id}",
                    orgId: orgId
                );
            }

            // Marcar como exitosa al final si no hubo errores
            respuesta.Exito = true;
        }
        catch (Exception ex)
        {
            string exceptionDetails = $"Error al obtener {typeof(TEntity).Name} con ID {id}: ";
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
                origen: $"Repo.{nameof(GetById)}<{typeof(TEntity).Name}>"
            );
            respuesta.MsnError.Add($"Error al obtener el registro: {ex.Message}");
            respuesta.Exito = false;
        }
        finally
        {
            semaphore.Release();
        }

        return respuesta;
    }

    
    private void ValidateUser(ApplicationUser elUser) 
    {
        if (elUser == null || string.IsNullOrEmpty(elUser.UserId))
            throw new InvalidOperationException("Se requiere un usuario válido para esta operación");
    }
    //Insert
    public virtual async Task<ApiRespAll<TEntity>> Insert(TEntity entity, string orgId,
            CancellationToken cancellationToken = default, ApplicationUser elUser = null)
    {
        var respuesta = new ApiRespAll<TEntity> {Exito = false, Varios = false};
        
        ValidateUser(elUser);
        
        if (entity == null)
        {
            await _repoBitacora.AddBitacora(
                userId: elUser.UserId,
                desc: "Intento de insertar entidad nula",
                orgId: orgId
            );
            respuesta.MsnError.Add("No se proporcionaron datos para insertar.");
            return respuesta;
        }  

        await semaphore.WaitAsync(cancellationToken);
        
        try
        {
            await dbset.AddAsync(entity);
            await context.SaveChangesAsync();
            
            respuesta.Exito = true;
            respuesta.DataUno = entity;
            
            await _repoBitacora.AddBitacora(
                userId: elUser.UserId,
                desc: $"Se insertó correctamente {typeof(TEntity).Name}",
                orgId: orgId
            );

            var (prefix, cacheKey) = GetCacheKey(orgId, typeof(TEntity).Name);
            UpdateLastUpdateTime(prefix, false);
        }
        catch (Exception ex)
        {
            string exceptionDetails = $"Error al insertar {typeof(TEntity).Name}: ";
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
                origen: $"Repo.{nameof(Insert)}<{typeof(TEntity).Name}>"
            );
            
            respuesta.MsnError.Add($"Error al intentar agregar un registro nuevo: {ex.Message}");
        }
        finally
        {
            semaphore.Release();
        }
        return respuesta;
    }

    //InsertPlus
    public virtual async Task<ApiRespAll<TEntity>> InsertPlus(
        IEnumerable<TEntity> entities,
        string orgId,
        CancellationToken cancellationToken = default,
        ApplicationUser elUser = null)
    {
        var respuesta = new ApiRespAll<TEntity> { Exito = false, Varios = true };
        
        ValidateUser(elUser);
        
        if (entities == null || !entities.Any())
        {
            await _repoBitacora.AddBitacora(
                userId: elUser.UserId,
                desc: "Intento de insertar colección vacía",
                orgId: orgId
            );
            respuesta.MsnError.Add("No se proporcionaron datos para insertar.");
            return respuesta;
        }

        await semaphore.WaitAsync(cancellationToken);
        try
        {
            await dbset.AddRangeAsync(entities);
            await context.SaveChangesAsync();
            
            respuesta.DataVarios = entities.ToList();
            
            await _repoBitacora.AddBitacora(
                userId: elUser.UserId,
                desc: $"Se insertaron correctamente {entities.Count()} registros de {typeof(TEntity).Name}",
                orgId: orgId
            );

            var (prefix, cacheKey) = GetCacheKey(orgId, typeof(TEntity).Name);
            UpdateLastUpdateTime(prefix, false);

            respuesta.Exito = true;
        }
        catch (Exception ex)
        {
            string exceptionDetails = $"Error al insertar múltiples {typeof(TEntity).Name}: ";
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
                origen: $"Repo.{nameof(InsertPlus)}<{typeof(TEntity).Name}>"
            );
            
            respuesta.MsnError.Add($"Error al intentar agregar los registros: {ex.Message}");
            respuesta.Exito = false;
        }
        finally
        {
            semaphore.Release();
        }
        return respuesta;
    }
    
    //Update
    public virtual async Task<ApiRespAll<TEntity>> Update(TEntity entity, string orgId,
            CancellationToken cancellationToken = default, ApplicationUser elUser = null)
    {
        var respuesta = new ApiRespAll<TEntity> {Exito = false, Varios = false};
        
        ValidateUser(elUser);
        
        if (entity == null)
        {
            await _repoBitacora.AddBitacora(
                userId: elUser.UserId,
                desc: "Intento de actualizar entidad nula",
                orgId: orgId
            );
            respuesta.MsnError.Add("No se proporcionaron datos para actualizar.");
            return respuesta;
        }

        await semaphore.WaitAsync(cancellationToken);
        try
        {
            dbset.Update(entity);
            await context.SaveChangesAsync();
            
            respuesta.DataUno = entity;
            
            await _repoBitacora.AddBitacora(
                userId: elUser.UserId,
                desc: $"Se actualizó correctamente {typeof(TEntity).Name}",
                orgId: orgId
            );

            var (prefix, cacheKey) = GetCacheKey(orgId, typeof(TEntity).Name);
            UpdateLastUpdateTime(prefix, false);

            // Marcar como exitoso al final
            respuesta.Exito = true;
        }
        catch (Exception ex)
        {
            string exceptionDetails = $"Error al actualizar {typeof(TEntity).Name}: ";
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
                origen: $"Repo.{nameof(Update)}<{typeof(TEntity).Name}>"
            );
            
            respuesta.MsnError.Add($"Error al intentar actualizar el registro: {ex.Message}");
            respuesta.Exito = false;
        }
        finally
        {
            semaphore.Release();
        }
        return respuesta;
    }

    //GetUserId
    public virtual async Task<ApplicationUser?> GetUserById(string id)
    {
        return await context.Set<ApplicationUser>().Include(nameof(ApplicationUser.Org))
                    .FirstOrDefaultAsync(e=>e.UserId == id);
    }


    //UpdateMisDatos
    public virtual async Task<ApiRespAll<TEntity>> UpdateMisDatos(TEntity entityToUpdate,
            string orgId, CancellationToken cancellationToken = default, ApplicationUser elUser = null)
    {
        var respuesta = new ApiRespAll<TEntity> { Exito = false, Varios = false };
        
        ValidateUser(elUser);

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

        var (prefix, cacheKey) = GetCacheKey(orgId, typeof(TEntity).Name);  
        await semaphore.WaitAsync(cancellationToken);
        
        try
        {
            var dbSet = context.Set<TEntity>();
            dbSet.Attach(entityToUpdate);
            context.Entry(entityToUpdate).State = EntityState.Modified;
            
            int affectedRows = await context.SaveChangesAsync(cancellationToken);
            
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

                UpdateLastUpdateTime(prefix, false);
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
    public virtual async Task<ApiRespAll<TEntity>> UpdatePlus(
        IEnumerable<TEntity> entities,
        string orgId,
        CancellationToken cancellationToken = default,
        ApplicationUser elUser = null)
    {
        var respuesta = new ApiRespAll<TEntity> {Exito = false, Varios = true};
        
        ValidateUser(elUser);
        
        if (entities == null || !entities.Any())
        {
            await _repoBitacora.AddBitacora(
                userId: elUser.UserId,
                desc: "Intento de actualizar colección vacía",
                orgId: orgId
            );
            respuesta.MsnError.Add("No se proporcionaron datos para actualizar.");
            return respuesta;
        }

        await semaphore.WaitAsync(cancellationToken);
        try
        {
            dbset.UpdateRange(entities);
            await context.SaveChangesAsync();
            
            respuesta.DataVarios = entities.ToList();
            
            await _repoBitacora.AddBitacora(
                userId: elUser.UserId,
                desc: $"Se actualizaron correctamente {entities.Count()} registros de {typeof(TEntity).Name}",
                orgId: orgId
            );

            var (prefix, cacheKey) = GetCacheKey(orgId, typeof(TEntity).Name);
            UpdateLastUpdateTime(prefix, false);

            // Marcar como exitoso al final
            respuesta.Exito = true;
        }
        catch (Exception ex)
        {
            string exceptionDetails = $"Error al actualizar múltiples {typeof(TEntity).Name}: ";
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
                origen: $"Repo.{nameof(UpdatePlus)}<{typeof(TEntity).Name}>"
            );
            
            respuesta.MsnError.Add($"Error al intentar actualizar los registros: {ex.Message}");
            respuesta.Exito = false;
        }
        finally
        {
            semaphore.Release();
        }
        return respuesta;
    }

    //DeletePLus

    //GetCount
    public virtual async Task<int> GetCount(string orgId, Expression<Func<TEntity, bool>> filtro = null,
        bool byPassCache = false, CancellationToken cancellationToken = default, ApplicationUser elUser = null)
    {
        int respuesta = -1;
        var (Prefix, cacheKey) = GetCacheKey(orgId, "Z900_Bitacora", filtro);
        if(!byPassCache && _globalCache.TryGetValue(cacheKey, out var cachedResultado))
        {
            var (cachedRespuesta, cacheTime, horaCaduca) = ((int, DateTime, TimeSpan)) cachedResultado;
            if (IsCacheValid(cacheKey, cacheTime, horaCaduca))
            {
                return cachedRespuesta;
            }
        }
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            var dbSet = context.Set<TEntity>();
            IQueryable<TEntity> querry = dbset;
            if (filtro != null)
            {
                querry = querry.Where(filtro);
            }

            int count = await querry.CountAsync();

            _globalCache[cacheKey] = (respuesta, DateTime.UtcNow, TimeSpan.FromMinutes(Min_actualizar));
            UpdateLastUpdateTime(Prefix, true);
            return count;
        }
        catch (Exception ex)
        {
            string exceptionDetails = $"Error al intentar contar numero de registros, ";
            exceptionDetails += $"Message: {ex.Message}\nStackTrace: {ex.StackTrace}\nData:\n";
            foreach (var key in ex.Data.Keys)
            {
                var value = ex.Data[key];
                exceptionDetails += $"{key}: {value}\n";
            }
            
            await _repoBitacora.AddLog(
                userId: elUser?.UserId ?? "Usuario NO identificado",
                orgId: orgId ?? "Sistema",
                desc: exceptionDetails,
                tipoLog: "Error",
                origen: $"Repo.{nameof(GetCount)}<{typeof(TEntity).Name}>"
            );
            return -1;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<bool> DeleteEntity(TEntity entityToDel, ApplicationUser elUser)
    {
        try 
        {
            // Validación de usuario
            ValidateUser(elUser);

            if (entityToDel == null)
            {
                await _repoBitacora.AddBitacora(
                    userId: elUser.UserId,
                    desc: $"Intento de eliminar entidad {typeof(TEntity).Name} nula",
                    orgId: elUser.OrgId
                );
                return false;
            }

            // Obtener OrgId de la entidad de forma segura
            var orgIdProperty = typeof(TEntity).GetProperty("OrgId");
            string orgId = orgIdProperty?.GetValue(entityToDel)?.ToString() ?? elUser.OrgId;

            // Usar el método completo que ya tiene toda la lógica de seguridad
            return await DeleteEntity(entityToDel, orgId, default, elUser);
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
                orgId: elUser?.OrgId ?? "Sistema",
                desc: exceptionDetails,
                tipoLog: "Error",
                origen: $"Repo.{nameof(DeleteEntity)}<{typeof(TEntity).Name}>"
            );
            return false;
        }
    }

    public virtual async Task<bool> Delete(TEntity entity, string orgId, 
            CancellationToken cancellationToken = default, ApplicationUser elUser = null)
    {
        ValidateUser(elUser);
        
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
            dbset.Remove(entity);
            await context.SaveChangesAsync();
            
            await _repoBitacora.AddBitacora(
                userId: elUser.UserId,
                desc: $"Se eliminó correctamente {typeof(TEntity).Name}",
                orgId: orgId
            );

            var (prefix, cacheKey) = GetCacheKey(orgId, typeof(TEntity).Name);
            UpdateLastUpdateTime(prefix, false);
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
        ValidateUser(elUser);

        if (id == null)
        {
            await _repoBitacora.AddBitacora(
                userId: elUser.UserId,
                desc: $"Intento de eliminar {typeof(TEntity).Name} con ID nulo",
                orgId: orgId
            );
            return false;
        }

        var (prefix, cacheKey) = GetCacheKey(orgId, typeof(TEntity).Name);
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            var entityToDel = await dbset.FindAsync(new[] { id }, cancellationToken);
            if (entityToDel == null)
            {
                await _repoBitacora.AddBitacora(
                    userId: elUser.UserId,
                    desc: $"Intento de eliminar {typeof(TEntity).Name} inexistente con ID: {id}",
                    orgId: orgId
                );
                return false;
            }

            dbset.Remove(entityToDel);
            int result = await context.SaveChangesAsync(cancellationToken);

            if (result > 0)
            {
                await _repoBitacora.AddBitacora(
                    userId: elUser.UserId,
                    desc: $"Se eliminó correctamente {typeof(TEntity).Name} con ID: {id}",
                    orgId: orgId
                );
                UpdateLastUpdateTime(prefix, false);
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

}

