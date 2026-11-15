using System.Linq.Expressions;

namespace Corralon.Logic.Models.Interfaces
{
    /// <summary>
    /// Repositorio genérico: operaciones comunes para cualquier entidad.
    /// Add / Update / Delete solo registran el cambio; quien guarda es el UnitOfWork (SaveChanges).
    /// </summary>
    public interface IRepository<T> where T : class
    {
        IQueryable<T> Query();
        Task<IEnumerable<T>> GetAll();
        Task<T?> GetById(int id);
        Task<IEnumerable<T>> Find(Expression<Func<T, bool>> predicate);
        Task Add(T entity);
        void Update(T entity);
        void Delete(T entity);
    }
}
