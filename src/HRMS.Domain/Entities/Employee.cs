using HRMS.Domain.Common;

namespace HRMS.Domain.Entities;

public class Employee : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public Guid OrganizationId { get; set; }

    public Organization? Organization { get; set; }

    /// <summary>The HR user who created this employee record. Used to scope edit/delete rights.</summary>
    public Guid CreatedByUserId { get; set; }

    public ApplicationUser? CreatedByUser { get; set; }

    /// <summary>
    /// Direct FK to the Roles table (AspNetRoles/"Roles") — always the "Employee"
    /// role's Id, set explicitly at creation (see EmployeeService.CreateEmployeeAsync).
    /// Every Employee record represents that role by definition, so this is never
    /// left unset or guessed — it's assigned the same explicit way Admin/HR RoleId
    /// values are assigned on ApplicationUser.
    /// </summary>
    public Guid RoleId { get; set; }
}
