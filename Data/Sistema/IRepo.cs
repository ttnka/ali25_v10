using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Ali25_V10.Data;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;  // Aquí deberían estar ApiRespuesta y ApiRespAll

namespace Ali25_V10.Data.Sistema;
public interface IRepo<TEntity> where TEntity : class
{
    // Get con filtros
    Task<ApiRespAll<TEntity>> Get(
        string orgId,
        ApplicationUser elUser,
        Expression<Func<TEntity, bool>> filtro = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderby = null,
        string propiedades = "",
        bool byPassCache = false,
        CancellationToken cancellationToken = default);

    // GetAll simplificado (usa OrgId del usuario)
    Task<ApiRespAll<TEntity>> GetAll(
        ApplicationUser elUser,
        bool byPassCache = false,
        CancellationToken cancellationToken = default);

    // GetById
    Task<ApiRespAll<TEntity>> GetById(
        object id, 
        string orgId,
        ApplicationUser elUser,
        bool byPassCache = false, 
        CancellationToken cancellationToken = default);

    // Delete simplificado (usa OrgId del usuario)
    Task<bool> DeleteEntity(
        TEntity entityToDel, 
        ApplicationUser elUser);

    // Insert
    Task<ApiRespAll<TEntity>> Insert(
        TEntity entity, 
        string orgId,
        ApplicationUser elUser,
        CancellationToken cancellationToken = default);

    // Update
    Task<ApiRespAll<TEntity>> Update(
        TEntity entityToUpdate,
        string orgId,
        ApplicationUser elUser,
        CancellationToken cancellationToken = default);

    // UpdateMisDatos
    Task<ApiRespAll<TEntity>> UpdateMisDatos(
        TEntity entityToUpdate, 
        string orgId,
        ApplicationUser elUser,
        CancellationToken cancellationToken = default);

    // Operaciones en lote
    Task<ApiRespAll<TEntity>> InsertPlus(
        IEnumerable<TEntity> entities, 
        string orgId,
        ApplicationUser elUser,
        CancellationToken cancellationToken = default);

    Task<ApiRespAll<TEntity>> UpdatePlus(
        IEnumerable<TEntity> entities, 
        string orgId,
        ApplicationUser elUser,
        CancellationToken cancellationToken = default);

    // Utilidades
    Task<int> GetCount(
        string orgId,
        ApplicationUser elUser,
        Expression<Func<TEntity, bool>> filtro = null,
        bool byPassCache = false, 
        CancellationToken cancellationToken = default);

    Task<ApplicationUser?> GetUserById(string id);
}