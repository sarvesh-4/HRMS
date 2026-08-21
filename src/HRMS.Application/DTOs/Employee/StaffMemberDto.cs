namespace HRMS.Application.DTOs.Employee;

/// <summary>
/// Which kind of staff to include in the combined Admin "view all staff" list
/// (GET /api/employees when the caller is an Admin). Defaults to All.
/// </summary>
public enum StaffType
{
    All,
    HR,
    Employee
}

/// <summary>
/// One row in the Admin's combined staff list — either an HR user account or an
/// Employee record, normalized into a single shape so both can be listed
/// side-by-side. Only Admins ever receive this DTO; HR still gets the plain
/// EmployeeResponseDto list exactly as before — this is purely additive.
/// </summary>
public class StaffMemberDto
{
    public Guid Id { get; set; }

    /// <summary>"HR" or "Employee".</summary>
    public string StaffType { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>Only populated for Employee rows — HR accounts don't have these fields.</summary>
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }

    public Guid OrganizationId { get; set; }

    /// <summary>For an HR row: the Admin who created them. For an Employee row: the HR who created them.</summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>Direct FK to the Roles table for this staff member's role.</summary>
    public Guid? RoleId { get; set; }

    /// <summary>Only meaningful for HR rows — an Employee record has no active/removed state.</summary>
    public bool? IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
