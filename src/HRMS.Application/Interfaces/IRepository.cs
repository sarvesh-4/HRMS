using HRMS.Domain.Common;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Generic EF Core repository used for writes (Create/Update/Delete) and simple
/// single-entity reads. List/reporting-style reads go through IEmployeeRepository (Dapper).
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<List<T>> GetAllAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
    Task<int> SaveChangesAsync();
}
