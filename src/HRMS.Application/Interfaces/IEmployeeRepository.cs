using HRMS.Domain.Entities;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Read-oriented repository implemented with Dapper (Infrastructure layer) to
/// demonstrate a lightweight micro-ORM alongside EF Core, which is used for writes.
/// </summary>
public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id);
    Task<IEnumerable<Employee>> GetByOrganizationAsync(Guid organizationId);
    Task<IEnumerable<Employee>> GetByCreatedByAsync(Guid createdByUserId);
}
