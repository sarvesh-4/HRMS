namespace HRMS.Domain.Constants;

/// <summary>
/// Fixed system role names. Keeping these as constants (instead of magic strings)
/// means [Authorize(Roles = AppRoles.Admin)] refactors safely everywhere.
///
/// Role model: there is NO default/fallback role. A record's RoleId (see
/// ApplicationUser.RoleId and Employee.RoleId) is only ever set by an explicit
/// action — becoming Admin by creating an org, being added as HR by an Admin,
/// or being created as an Employee record by HR. A brand-new self-registered
/// user has RoleId = null until they take one of those actions; nothing ever
/// silently defaults it to anything.
///
/// To add a new role in the future (e.g. "Sales"): add its constant below and
/// add it to <see cref="All"/> — RoleSeeder picks it up automatically. Wherever
/// that role gets assigned, look its Guid Id up from the Roles table (via
/// RoleManager) and set RoleId explicitly, the same way OrganizationService,
/// HrUserService, and EmployeeService do for Admin/HR/Employee.
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string HR = "HR";

    /// <summary>
    /// Not a fallback/default — this is the role explicitly assigned to every
    /// Employee business record (see EmployeeService.CreateEmployeeAsync). It is
    /// never auto-assigned to an ApplicationUser (login account).
    /// </summary>
    public const string Employee = "Employee";

    /// <summary>Every role that should exist in the database — seeded on startup by RoleSeeder.</summary>
    public static readonly string[] All = { Admin, HR, Employee };
}
