using Microsoft.AspNetCore.Identity;

namespace HRMS.Domain.Entities;

/// <summary>
/// Extends ASP.NET Core Identity's user with the fields the HRMS domain needs.
/// Guid is used as the key type across the whole app for consistency.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Null until the user creates (Admin) or is added to (HR) an organization.
    /// </summary>
    public Guid? OrganizationId { get; set; }

    public Organization? Organization { get; set; }

    /// <summary>
    /// For HR users: the Admin who created them. Null for the Admin (org creator) itself.
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    public ApplicationUser? CreatedByUser { get; set; }

    /// <summary>
    /// Soft "removed" flag. When an Admin removes an HR user we deactivate + lock the
    /// account instead of hard-deleting it, since employees still reference it as creator.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Direct FK to the Roles table (AspNetRoles/"Roles") for whichever single
    /// role this account holds. Null until explicitly assigned — set to the
    /// Admin role's Id when this user creates their first organization (see
    /// OrganizationService.CreateOrganizationAsync), or to the HR role's Id when
    /// an Admin creates them as HR (see HrUserService.CreateHrUserAsync). Never
    /// defaulted to anything — a freshly registered user has RoleId = null.
    /// </summary>
    public Guid? RoleId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
