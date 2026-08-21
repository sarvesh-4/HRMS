using HRMS.Application.DTOs.Employee;

namespace HRMS.Application.Interfaces;

public interface IEmployeeService
{
    /// <summary>HR only. New employee is stamped with the current HR user as creator.</summary>
    Task<EmployeeResponseDto> CreateEmployeeAsync(CreateEmployeeDto dto);

    /// <summary>HR only, and only for employees they created.</summary>
    Task<EmployeeResponseDto> UpdateEmployeeAsync(Guid id, UpdateEmployeeDto dto);

    /// <summary>HR only, and only for employees they created.</summary>
    Task DeleteEmployeeAsync(Guid id);

    /// <summary>Admin: any employee in the org. HR: only their own.</summary>
    Task<EmployeeResponseDto> GetByIdAsync(Guid id);

    /// <summary>Admin: all employees in the org. HR: only the ones they created.</summary>
    Task<List<EmployeeResponseDto>> GetAllAsync();

    /// <summary>
    /// Admin only. Combined "staff directory" across one or all organizations the
    /// Admin owns, covering both HR accounts and Employee records, filterable by
    /// <see cref="StaffType"/>. HR calling GET /api/employees never reaches this —
    /// they keep using <see cref="GetAllAsync"/> above, unchanged.
    /// </summary>
    /// <param name="organizationId">
    /// Optional. Narrows results to one organization the Admin owns. When null,
    /// results are aggregated across every organization the Admin owns.
    /// </param>
    /// <param name="staffType">Which kind of staff to include. Defaults to All.</param>
    Task<List<StaffMemberDto>> GetAllStaffAsync(Guid? organizationId, StaffType staffType);

    /// <summary>
    /// Admin only. Looks up a single staff member by id — checking both HR
    /// accounts and Employee records — across any organization the Admin owns.
    /// Same search scope as <see cref="GetAllStaffAsync"/>, narrowed to one id.
    /// </summary>
    Task<StaffMemberDto> GetStaffByIdAsync(Guid id);
}
