namespace HRMS.Domain.Common;

/// <summary>
/// Common auditable fields shared by every domain entity (except ApplicationUser,
/// which already inherits its own key/audit fields from IdentityUser&lt;Guid&gt;).
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
