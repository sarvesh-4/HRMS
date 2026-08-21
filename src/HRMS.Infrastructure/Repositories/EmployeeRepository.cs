using Dapper;
using HRMS.Application.Interfaces;
using HRMS.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace HRMS.Infrastructure.Repositories;

/// <summary>
/// Dapper implementation of the employee read repository. Demonstrated here (instead
/// of EF Core) to show a lightweight, hand-written-SQL path for read-heavy list
/// queries, while EF Core (see Repository&lt;T&gt;) continues to own the writes.
/// </summary>
public class EmployeeRepository : IEmployeeRepository
{
    private readonly string _connectionString;

    public EmployeeRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<Employee?> GetByIdAsync(Guid id)
    {
        const string sql = """
            SELECT "Id", "FirstName", "LastName", "Email", "PhoneNumber", "Address",
                   "OrganizationId", "CreatedByUserId", "RoleId", "CreatedAt", "UpdatedAt"
            FROM "Employees"
            WHERE "Id" = @Id
            """;

        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Employee>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Employee>> GetByOrganizationAsync(Guid organizationId)
    {
        const string sql = """
            SELECT "Id", "FirstName", "LastName", "Email", "PhoneNumber", "Address",
                   "OrganizationId", "CreatedByUserId", "RoleId", "CreatedAt", "UpdatedAt"
            FROM "Employees"
            WHERE "OrganizationId" = @OrganizationId
            ORDER BY "CreatedAt" DESC
            """;

        using var connection = CreateConnection();
        return await connection.QueryAsync<Employee>(sql, new { OrganizationId = organizationId });
    }

    public async Task<IEnumerable<Employee>> GetByCreatedByAsync(Guid createdByUserId)
    {
        const string sql = """
            SELECT "Id", "FirstName", "LastName", "Email", "PhoneNumber", "Address",
                   "OrganizationId", "CreatedByUserId", "RoleId", "CreatedAt", "UpdatedAt"
            FROM "Employees"
            WHERE "CreatedByUserId" = @CreatedByUserId
            ORDER BY "CreatedAt" DESC
            """;

        using var connection = CreateConnection();
        return await connection.QueryAsync<Employee>(sql, new { CreatedByUserId = createdByUserId });
    }
}
