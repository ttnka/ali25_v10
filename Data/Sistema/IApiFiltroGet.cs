using System.Linq.Expressions;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Ali25_V10.Data.Sistema;
public interface IApiFiltroGet<TEntity> : IRepo<TEntity> where TEntity : class
{
    Task<IEnumerable<TEntity>> Get(
        Expression<Func<TEntity, bool>> filtro = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderby = null,
        string propiedades = "");
}